using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using Core.Application.Abstractions;
using Core.Application.Authorization;
using Core.Application.Common;
using Core.Application.DTOs;
using Core.Application.Mappers;
using Core.Application.Requests;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Services;

public sealed class GuaranteeRenewalAppService(
    ICoreUnitOfWork unitOfWork,
    ICoreDbContext dbContext,
    IGuaranteeCaseNumberGenerator caseNumberGenerator,
    IGuaranteeWorkflowOrchestrator workflowOrchestrator,
    IGuaranteeAuthorizationService authorizationService,
    IGuaranteeCaseDtoMapper dtoMapper,
    IUserDisplayLookup userDisplayLookup,
    ILogger<GuaranteeRenewalAppService> logger) : IGuaranteeRenewalAppService
{
    public async Task<Result<GuaranteeRenewalDto>> CreateAsync(CreateGuaranteeRenewalRequest request, CancellationToken ct)
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result<GuaranteeRenewalDto>.Fail(auth.Error!);

        if (!authorizationService.HasPermission(GuaranteePermissions.Create))
            return Result<GuaranteeRenewalDto>.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        var parent = await unitOfWork.GuaranteeCases.GetScopedAsync(
            request.ParentGuaranteeCaseId,
            auth.Value!,
            authorizationService.IsInternalUser,
            ct);

        if (parent is null)
            return Result<GuaranteeRenewalDto>.Fail(Error.NotFound(ApiMessages.GuaranteeCaseNotFound));

        if (parent.CurrentStatus != GuaranteeCaseStatus.Completed)
            return Result<GuaranteeRenewalDto>.Fail(Error.Conflict(ApiMessages.ParentGuaranteeNotEligibleForRenewal));

        var caseNumber = await caseNumberGenerator.GenerateRenewalCaseAsync(ct);
        var entity = new GuaranteeRenewalCase(
            caseNumber,
            auth.Value!,
            parent.Id,
            request.RenewalKind,
            request.RequestedExpiryDate,
            request.RequestedAmount);

        var workflowId = await workflowOrchestrator.StartRenewalCaseAsync(entity.Id, ct);
        entity.AttachWorkflowInstance(workflowId);

        await unitOfWork.GuaranteeRenewals.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var loaded = await unitOfWork.GuaranteeRenewals.GetAsync(entity.Id, ct);
        return Result<GuaranteeRenewalDto>.Ok(await MapRenewalAsync(loaded!, ct));
    }

    public async Task<Result<GuaranteeRenewalDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result<GuaranteeRenewalDto>.Fail(auth.Error!);

        var entity = await unitOfWork.GuaranteeRenewals.GetScopedAsync(
            id, auth.Value!, authorizationService.IsInternalUser, ct);

        if (entity is null)
            return Result<GuaranteeRenewalDto>.Fail(Error.NotFound(ApiMessages.GuaranteeRenewalNotFound));

        return Result<GuaranteeRenewalDto>.Ok(await MapRenewalAsync(entity, ct));
    }

    private async Task<GuaranteeRenewalDto> MapRenewalAsync(GuaranteeRenewalCase renewal, CancellationToken ct)
    {
        var rawContext = await dbContext.GuaranteeRenewalCases
            .AsNoTracking()
            .Where(x => x.Id == renewal.Id)
            .Select(x => new
            {
                x.ApplicantUserId,
                ParentBeneficiaryName = x.ParentGuaranteeCase.Application != null
                    ? x.ParentGuaranteeCase.Application.BeneficiaryName
                    : null,
                ParentCompanyName = x.ParentGuaranteeCase.ApplicantCompany != null
                    ? x.ParentGuaranteeCase.ApplicantCompany.Name
                    : null
            })
            .FirstAsync(ct);

        var applicantLookup = await userDisplayLookup.GetByIdsAsync([rawContext.ApplicantUserId], ct);
        var context = new GuaranteeRenewalContextProjection(
            rawContext.ApplicantUserId,
            rawContext.ParentBeneficiaryName,
            rawContext.ParentCompanyName,
            userDisplayLookup.ResolveFullName(applicantLookup, rawContext.ApplicantUserId));

        return dtoMapper.MapRenewal(renewal, context);
    }

    public async Task<Result> SubmitAsync(Guid id, CancellationToken ct)
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result.Fail(auth.Error!);

        var entity = await unitOfWork.GuaranteeRenewals.GetScopedAsync(
            id, auth.Value!, authorizationService.IsInternalUser, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.GuaranteeRenewalNotFound));

        if (entity.CurrentStatus != GuaranteeRenewalStatus.Draft)
            return Result.Fail(Error.Conflict(ApiMessages.InvalidTransition));

        entity.TransitionTo(GuaranteeRenewalStatus.CeoReview, auth.Value!);
        await unitOfWork.SaveChangesAsync(ct);
        await SignalSafeAsync(id, ct);
        return Result.Ok();
    }

    public async Task<Result> CeoApproveAsync(Guid id, CancellationToken ct)
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result.Fail(auth.Error!);

        if (!authorizationService.HasPermission(GuaranteePermissions.CeoApprove))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        var entity = await unitOfWork.GuaranteeRenewals.GetScopedAsync(id, auth.Value!, true, ct);
        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.GuaranteeRenewalNotFound));

        if (entity.CurrentStatus != GuaranteeRenewalStatus.CeoReview)
            return Result.Fail(Error.Conflict(ApiMessages.InvalidTransition));

        entity.TransitionTo(GuaranteeRenewalStatus.CreditDateUpdate, auth.Value!);
        await unitOfWork.SaveChangesAsync(ct);
        await SignalSafeAsync(id, ct);
        return Result.Ok();
    }

    public async Task<Result> CeoRejectAsync(Guid id, string reason, CancellationToken ct)
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result.Fail(auth.Error!);

        if (!authorizationService.HasPermission(GuaranteePermissions.CeoApprove))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        var entity = await unitOfWork.GuaranteeRenewals.GetScopedAsync(id, auth.Value!, true, ct);
        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.GuaranteeRenewalNotFound));

        if (entity.CurrentStatus != GuaranteeRenewalStatus.CeoReview)
            return Result.Fail(Error.Conflict(ApiMessages.InvalidTransition));

        entity.TransitionTo(GuaranteeRenewalStatus.Rejected, auth.Value!);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> UpdateCreditDatesAsync(Guid id, UpdateGuaranteeRenewalDatesRequest request, CancellationToken ct)
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result.Fail(auth.Error!);

        if (!authorizationService.HasPermission(GuaranteePermissions.ManageApprovalForm))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        var entity = await unitOfWork.GuaranteeRenewals.GetScopedAsync(id, auth.Value!, true, ct);
        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.GuaranteeRenewalNotFound));

        if (entity.CurrentStatus != GuaranteeRenewalStatus.CreditDateUpdate)
            return Result.Fail(Error.Conflict(ApiMessages.InvalidTransition));

        entity.SetApprovedExpiryDate(request.ApprovedExpiryDate);
        entity.TransitionTo(GuaranteeRenewalStatus.Completed, auth.Value!);
        await unitOfWork.SaveChangesAsync(ct);
        await SignalSafeAsync(id, ct);
        return Result.Ok();
    }

    private async Task SignalSafeAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await workflowOrchestrator.SignalRenewalCaseAsync(id, WorkflowSignals.StatusChanged, null, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Renewal workflow signal failed for {RenewalId}", id);
        }
    }
}

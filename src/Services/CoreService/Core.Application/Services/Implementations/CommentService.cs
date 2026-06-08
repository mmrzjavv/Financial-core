using BuildingBlocks.Application.Validation;
using Core.Application.Common;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Application.Contracts.Comments;
using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Services.Implementations;

public sealed class CommentService : ICommentService
{
    private readonly ICoreDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IValidator<AddCommentRequest> _validator;
    private readonly ICaseWorkflowOrchestrator _workflowOrchestrator;

    public CommentService(
        ICoreDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        IValidator<AddCommentRequest> validator,
        ICaseWorkflowOrchestrator workflowOrchestrator)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _validator = validator;
        _workflowOrchestrator = workflowOrchestrator;
    }

    public async Task<Result> AddCommentAsync(Guid caseId, AddCommentRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        var caseMeta = await _db.InvestmentCases
            .AsNoTracking()
            .Where(x => x.Id == caseId)
            .Select(x => new { x.ApplicantUserId })
            .FirstOrDefaultAsync(ct);

        if (caseMeta is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        var isApplicant = caseMeta.ApplicantUserId == _currentUser.UserId;
        if (isApplicant && request.IsInternal)
            return Result.Fail(Error.Forbidden(ApiMessages.ApplicantsCannotCreateInternalComments));

        if (!isApplicant && !_currentUser.Roles.Contains(UserRoleClaims.Admin))
            return Result.Fail(Error.Forbidden());

        await _db.CaseComments.AddAsync(
            new InvestmentCaseComment(
                caseId,
                request.Phase,
                _currentUser.UserId,
                senderRole: _currentUser.Roles.FirstOrDefault(),
                request.Message,
                isRevisionRequest: false,
                isInternal: request.IsInternal),
            ct);

        await _db.InvestmentCases.TouchUpdatedAtAsync(caseId, _clock.UtcNow, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RequestRevisionAsync(Guid caseId, AddCommentRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        if (_currentUser.Roles.Contains(UserRoleClaims.Applicant))
            return Result.Fail(Error.Forbidden(ApiMessages.ApplicantsCannotRequestRevisions));

        var entity = await _db.InvestmentCases
            .AsSplitQuery()
            .Include(x => x.Comments)
            .Include(x => x.WorkflowHistory)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        if (entity.CurrentPhase != request.Phase)
            return Result.Fail(Error.Conflict(ApiMessages.RevisionMustMatchPhase));

        entity.RequestRevision(request.Phase, _currentUser.UserId, request.Message, request.IsInternal);

        if (_db is DbContext efContext)
            efContext.Entry(entity).State = EntityState.Unchanged;

        await _db.InvestmentCases.ApplyStateAsync(
            caseId,
            entity.CurrentStatus,
            entity.CurrentPhase,
            entity.UpdatedAt ?? _clock.UtcNow,
            entity.CompletedAt,
            ct);

        try
        {
            await _workflowOrchestrator.SignalAsync(caseId, WorkflowSignals.StatusChanged, null, ct);
        }
        catch
        {
            // Domain state persisted below; Elsa sync is best-effort.
        }

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

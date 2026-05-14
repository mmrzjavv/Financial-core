using BuildingBlocks.Application.Validation;
using Core.Application.Common;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Application.Contracts.Comments;
using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Enums;



namespace Services.CoreService.Core.Application.Services.Implementations;

public sealed class CommentService : ICommentService
{
    private readonly ICoreDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<AddCommentRequest> _validator;
    private readonly ICaseWorkflowOrchestrator _workflowOrchestrator;

    public CommentService(
        ICoreDbContext db,
        ICurrentUser currentUser,
        IValidator<AddCommentRequest> validator,
        ICaseWorkflowOrchestrator workflowOrchestrator)
    {
        _db = db;
        _currentUser = currentUser;
        _validator = validator;
        _workflowOrchestrator = workflowOrchestrator;
    }

    public async Task<Result> AddCommentAsync(Guid caseId, AddCommentRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        var entity = await _db.InvestmentCases
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        var isApplicant = entity.ApplicantUserId == _currentUser.UserId;
        if (isApplicant && request.IsInternal)
            return Result.Fail(Error.Forbidden(ApiMessages.ApplicantsCannotCreateInternalComments));

        if (!isApplicant && !_currentUser.Roles.Contains(SystemRoles.Admin))
            return Result.Fail(Error.Forbidden());

        entity.Comments.Add(new Domain.Entities.CaseComment(
            caseId,
            request.Phase,
            _currentUser.UserId,
            senderRole: _currentUser.Roles.FirstOrDefault(),
            request.Message,
            isRevisionRequest: false,
            isInternal: request.IsInternal));

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RequestRevisionAsync(Guid caseId, AddCommentRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        var entity = await _db.InvestmentCases
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        if (entity.CurrentPhase != request.Phase)
            return Result.Fail(Error.Conflict(ApiMessages.RevisionMustMatchPhase));

        if (_currentUser.Roles.Contains(SystemRoles.Applicant))
            return Result.Fail(Error.Forbidden(ApiMessages.ApplicantsCannotRequestRevisions));

        entity.RequestRevision(request.Phase, _currentUser.UserId, request.Message, request.IsInternal);

        await _workflowOrchestrator.SignalAsync(caseId, WorkflowSignals.StatusChanged, new { caseId, request.Phase }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
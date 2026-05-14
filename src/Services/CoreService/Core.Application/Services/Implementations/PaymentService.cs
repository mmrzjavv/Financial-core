using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using Core.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Application.Contracts.Payments;
using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Services.Implementations;

public sealed class PaymentService : IPaymentService
{
    private readonly ICoreDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<RecordPaymentRequest> _validator;
    private readonly ICaseWorkflowOrchestrator _workflowOrchestrator;

    public PaymentService(
        ICoreDbContext db,
        ICurrentUser currentUser,
        IValidator<RecordPaymentRequest> validator,
        ICaseWorkflowOrchestrator workflowOrchestrator)
    {
        _db = db;
        _currentUser = currentUser;
        _validator = validator;
        _workflowOrchestrator = workflowOrchestrator;
    }

    public async Task<Result> RecordAsync(Guid caseId, RecordPaymentRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToString()));

        if (!_currentUser.Roles.Contains(SystemRoles.FinancialExpert) && !_currentUser.Roles.Contains(SystemRoles.Admin))
            return Result.Fail(Error.Forbidden());

        var entity = await _db.InvestmentCases
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound("Case not found."));

        if (entity.CurrentPhase != CasePhase.PaymentProcessing)
            return Result.Fail(Error.Conflict("Case is not in Payment Processing phase."));

        entity.AddPayment(
            request.Amount,
            request.PaymentDate,
            request.TransactionNumber,
            request.ReceiptS3Key,
            request.Notes,
            _currentUser.UserId);

        entity.ChangePhase(CasePhase.Completion, CaseStatus.Completed, _currentUser.UserId, "Payment recorded");
        await _workflowOrchestrator.SignalAsync(caseId, WorkflowSignals.StatusChanged, new { caseId, request.Amount }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

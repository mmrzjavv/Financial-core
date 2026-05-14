using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Application.Contracts.Finance;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Services.Implementations;

public sealed class FinancialWorksheetService : IFinancialWorksheetService
{
    private readonly ICoreDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<FinancialWorksheetUpsertRequest> _validator;

    public FinancialWorksheetService(
        ICoreDbContext db,
        ICurrentUser currentUser,
        IValidator<FinancialWorksheetUpsertRequest> validator)
    {
        _db = db;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result> UpsertAsync(Guid caseId, FinancialWorksheetUpsertRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToString()));

        var entity = await _db.InvestmentCases
            .Include(x => x.FinancialWorksheet)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound("Case not found."));

        if (entity.ApplicantUserId != _currentUser.UserId)
            return Result.Fail(Error.Forbidden());

        if (entity.CurrentPhase != CasePhase.FinancialWorksheet)
            return Result.Fail(Error.Conflict("Financial Worksheet is not the current phase."));

        var isNew = entity.FinancialWorksheet is null;
        var worksheet = entity.UpsertFinancialWorksheet(
            request.BankName,
            request.IBAN,
            request.ApprovedAmount,
            request.PaymentSchedule,
            request.Notes);
        if (isNew)
            await _db.FinancialWorksheets.AddAsync(worksheet, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

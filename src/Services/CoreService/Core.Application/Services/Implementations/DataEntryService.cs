using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Application.Contracts.DataEntry;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Services.Implementations;

public sealed class DataEntryService : IDataEntryService
{
    private readonly ICoreDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<DataEntry1UpsertRequest> _de1Validator;
    private readonly IValidator<DataEntry2UpsertRequest> _de2Validator;

    public DataEntryService(
        ICoreDbContext db,
        ICurrentUser currentUser,
        IValidator<DataEntry1UpsertRequest> de1Validator,
        IValidator<DataEntry2UpsertRequest> de2Validator)
    {
        _db = db;
        _currentUser = currentUser;
        _de1Validator = de1Validator;
        _de2Validator = de2Validator;
    }

    public async Task<Result> UpsertDataEntry1Async(Guid caseId, DataEntry1UpsertRequest request, CancellationToken ct)
    {
        var validation = await _de1Validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToString()));

        var entity = await _db.InvestmentCases
            .Include(x => x.DataEntry1)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound("Case not found."));

        if (entity.ApplicantUserId != _currentUser.UserId)
            return Result.Fail(Error.Forbidden());

        if (entity.CurrentPhase != CasePhase.DataEntry1)
            return Result.Fail(Error.Conflict("Data Entry 1 is not the current phase."));

        var isNew = entity.DataEntry1 is null;
        var entry = entity.UpsertDataEntry1(
            request.StartupTitle,
            request.BusinessDescription,
            request.RequestedAmount,
            request.TeamSize,
            request.Website,
            request.Country,
            request.City,
            request.Industry);
        if (isNew)
            await _db.DataEntry1.AddAsync(entry, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> UpsertDataEntry2Async(Guid caseId, DataEntry2UpsertRequest request, CancellationToken ct)
    {
        var validation = await _de2Validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToString()));

        var entity = await _db.InvestmentCases
            .Include(x => x.DataEntry2)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound("Case not found."));

        if (entity.ApplicantUserId != _currentUser.UserId)
            return Result.Fail(Error.Forbidden());

        if (entity.CurrentPhase != CasePhase.DataEntry2)
            return Result.Fail(Error.Conflict("Data Entry 2 is not the current phase."));

        var isNew = entity.DataEntry2 is null;
        var entry = entity.UpsertDataEntry2(
            request.MarketAnalysis,
            request.RevenueModel,
            request.CompetitiveAdvantage,
            request.FinancialProjection,
            request.Risks,
            request.GoToMarketStrategy);
        if (isNew)
            await _db.DataEntry2.AddAsync(entry, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

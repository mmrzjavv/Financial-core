using BuildingBlocks.Application.Validation;
using Core.Application.Common;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Application.Contracts.DataEntry;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Services.Implementations;

public sealed class DataEntryService : IDataEntryService
{
    private readonly ICoreDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IValidator<DataEntry1UpsertRequest> _de1Validator;
    private readonly IValidator<DataEntry2UpsertRequest> _de2Validator;

    public DataEntryService(
        ICoreDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        IValidator<DataEntry1UpsertRequest> de1Validator,
        IValidator<DataEntry2UpsertRequest> de2Validator)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _de1Validator = de1Validator;
        _de2Validator = de2Validator;
    }

    public async Task<Result> UpsertDataEntry1Async(Guid caseId, DataEntry1UpsertRequest request, CancellationToken ct)
    {
        var validation = await _de1Validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        var caseMeta = await _db.InvestmentCases
            .AsNoTracking()
            .Where(x => x.Id == caseId)
            .Select(x => new { x.ApplicantUserId, x.CurrentPhase })
            .FirstOrDefaultAsync(ct);

        if (caseMeta is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        if (caseMeta.ApplicantUserId != _currentUser.UserId)
            return Result.Fail(Error.Forbidden());

        if (caseMeta.CurrentPhase != CasePhase.Application)
            return Result.Fail(Error.Conflict(ApiMessages.DataEntry1NotCurrentPhase));

        var dataEntry = await _db.DataEntry1.FirstOrDefaultAsync(x => x.CaseId == caseId, ct);
        if (dataEntry is null)
        {
            dataEntry = new InvestmentCaseDataEntry1(
                caseId,
                request.StartupTitle,
                request.BusinessDescription,
                request.RequestedAmount,
                request.TeamSize,
                request.Website);
            dataEntry.Update(
                request.StartupTitle,
                request.BusinessDescription,
                request.RequestedAmount,
                request.TeamSize,
                request.Website,
                request.Country,
                request.City,
                request.Industry);
            await _db.DataEntry1.AddAsync(dataEntry, ct);
        }
        else
        {
            dataEntry.Update(
                request.StartupTitle,
                request.BusinessDescription,
                request.RequestedAmount,
                request.TeamSize,
                request.Website,
                request.Country,
                request.City,
                request.Industry);
        }

        await _db.InvestmentCases.TouchUpdatedAtAsync(caseId, _clock.UtcNow, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> UpsertDataEntry2Async(Guid caseId, DataEntry2UpsertRequest request, CancellationToken ct)
    {
        var validation = await _de2Validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        var caseMeta = await _db.InvestmentCases
            .AsNoTracking()
            .Where(x => x.Id == caseId)
            .Select(x => new { x.ApplicantUserId, x.CurrentPhase })
            .FirstOrDefaultAsync(ct);

        if (caseMeta is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        if (caseMeta.ApplicantUserId != _currentUser.UserId)
            return Result.Fail(Error.Forbidden());

        if (caseMeta.CurrentPhase != CasePhase.Application)
            return Result.Fail(Error.Conflict(ApiMessages.DataEntry2NotCurrentPhase));

        var dataEntry = await _db.DataEntry2.FirstOrDefaultAsync(x => x.CaseId == caseId, ct);
        if (dataEntry is null)
        {
            await _db.DataEntry2.AddAsync(
                new InvestmentCaseDataEntry2(
                    caseId,
                    request.MarketAnalysis,
                    request.RevenueModel,
                    request.CompetitiveAdvantage,
                    request.FinancialProjection),
                ct);
        }
        else
        {
            dataEntry.Update(
                request.MarketAnalysis,
                request.RevenueModel,
                request.CompetitiveAdvantage,
                request.FinancialProjection,
                request.Risks,
                request.GoToMarketStrategy);
        }

        await _db.InvestmentCases.TouchUpdatedAtAsync(caseId, _clock.UtcNow, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

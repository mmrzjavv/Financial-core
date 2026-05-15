using Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;
using Services.CoreService.Core.Persistence;

namespace Core.Infrastructure.Persistence;

public sealed class InvestmentCaseRepository(CoreDbContext dbContext) : IInvestmentCaseRepository
{
    public Task<InvestmentCase?> GetAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.InvestmentCases
            .Include(x => x.ApplicantCompany)
            .Include(x => x.DataEntry1)
            .Include(x => x.DataEntry2)
            .Include(x => x.FinancialWorksheet)
            .Include(x => x.Documents)
            .Include(x => x.Comments)
            .Include(x => x.Revisions)
            .Include(x => x.Evaluations).ThenInclude(x => x.Items)
            .Include(x => x.Valuations)
            .Include(x => x.Payments)
            .Include(x => x.WorkflowHistory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<InvestmentCase?> GetScopedAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken)
    {
        var query = dbContext.InvestmentCases
            .Include(x => x.ApplicantCompany)
            .Include(x => x.DataEntry1)
            .Include(x => x.DataEntry2)
            .Include(x => x.FinancialWorksheet)
            .Include(x => x.Documents)
            .Include(x => x.Comments)
            .Include(x => x.Revisions)
            .Include(x => x.Evaluations).ThenInclude(x => x.Items)
            .Include(x => x.Valuations)
            .Include(x => x.Payments)
            .Include(x => x.WorkflowHistory)
            .AsQueryable();

        if (!isInternalUser)
            query = query.Where(x => x.ApplicantUserId == userId);

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<InvestmentCase?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken)
        => dbContext.InvestmentCases.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber, cancellationToken);

    public Task AddAsync(InvestmentCase investmentCase, CancellationToken cancellationToken)
        => dbContext.InvestmentCases.AddAsync(investmentCase, cancellationToken).AsTask();

    public Task<bool> ExistsCaseNumberAsync(string caseNumber, CancellationToken cancellationToken)
        => dbContext.InvestmentCases.AnyAsync(x => x.CaseNumber == caseNumber, cancellationToken);

    public async Task<IEnumerable<InvestmentCase>> SearchAsync(
        string? caseNumber,
        string? applicantUserId,
        CasePhase? phase,
        CaseStatus? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InvestmentCases
            .AsNoTracking()
            .Include(x => x.ApplicantCompany)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(caseNumber))
            query = query.Where(x => x.CaseNumber.Contains(caseNumber));

        if (!string.IsNullOrWhiteSpace(applicantUserId))
            query = query.Where(x => x.ApplicantUserId == applicantUserId);

        if (phase.HasValue)
            query = query.Where(x => x.CurrentPhase == phase.Value);

        if (status.HasValue)
            query = query.Where(x => x.CurrentStatus == status.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InvestmentCase>> SearchScopedAsync(
        string? caseNumber,
        string? applicantUserId,
        CasePhase? phase,
        CaseStatus? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InvestmentCases
            .AsNoTracking()
            .Include(x => x.ApplicantCompany)
            .AsQueryable();

        if (!isInternalUser)
            query = query.Where(x => x.ApplicantUserId == userId);

        if (!string.IsNullOrWhiteSpace(caseNumber))
            query = query.Where(x => x.CaseNumber.Contains(caseNumber));

        if (isInternalUser && !string.IsNullOrWhiteSpace(applicantUserId))
            query = query.Where(x => x.ApplicantUserId == applicantUserId);

        if (phase.HasValue)
            query = query.Where(x => x.CurrentPhase == phase.Value);

        if (status.HasValue)
            query = query.Where(x => x.CurrentStatus == status.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}

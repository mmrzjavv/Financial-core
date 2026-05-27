using Core.Application.Abstractions;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence;

public sealed class InvestmentCaseRepository(CoreDbContext dbContext) : IInvestmentCaseRepository
{
    public Task<InvestmentCase?> GetAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.InvestmentCases
            .AsSplitQuery()
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
        => ApplyScopedFilter(
                dbContext.InvestmentCases
                    .AsSplitQuery()
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
                    .Include(x => x.WorkflowHistory),
                userId,
                isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<InvestmentCase?> GetScopedForTransitionAsync(
        Guid id,
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
        => ApplyScopedFilter(
                dbContext.InvestmentCases
                    .AsSplitQuery()
                    .Include(x => x.DataEntry1)
                    .Include(x => x.DataEntry2)
                    .Include(x => x.FinancialWorksheet)
                    .Include(x => x.Documents)
                    .Include(x => x.Payments)
                    .Include(x => x.WorkflowHistory),
                userId,
                isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<string?> GetWorkflowInstanceIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.InvestmentCases
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.WorkflowInstanceId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<InvestmentCase?> GetScopedWithDocumentsAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken)
        => ApplyScopedFilter(
                dbContext.InvestmentCases
                    .Include(x => x.Documents),
                userId,
                isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private static IQueryable<InvestmentCase> ApplyScopedFilter(
        IQueryable<InvestmentCase> query,
        string userId,
        bool isInternalUser)
    {
        if (!isInternalUser)
            query = query.Where(x => x.ApplicantUserId == userId);

        return query;
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

    public async Task<IReadOnlyList<KanbanCaseProjection>> ListActiveKanbanProjectionsAsync(
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
    {
        var terminalStatuses = new[]
        {
            CaseStatus.Completed,
            CaseStatus.Rejected,
            CaseStatus.Cancelled,
            CaseStatus.Archived
        };

        var query = dbContext.InvestmentCases
            .AsNoTracking()
            .Where(x => !terminalStatuses.Contains(x.CurrentStatus));

        if (!isInternalUser)
            query = query.Where(x => x.ApplicantUserId == userId);

        return await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new KanbanCaseProjection(
                x.Id,
                x.CaseNumber,
                x.ApplicantType,
                x.CurrentPhase,
                x.CurrentStatus,
                x.CreatedAt,
                x.UpdatedAt,
                x.DataEntry1 != null ? x.DataEntry1.RepresentativeFullName : null,
                x.ApplicantCompany != null ? x.ApplicantCompany.Name : null))
            .ToListAsync(cancellationToken);
    }
}

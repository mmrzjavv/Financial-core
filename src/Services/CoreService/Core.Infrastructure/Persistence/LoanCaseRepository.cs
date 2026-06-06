using Core.Application.Abstractions;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence;

public sealed class LoanCaseRepository(CoreDbContext dbContext) : ILoanCaseRepository
{
    public Task<LoanCase?> GetAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.LoanCases
            .AsSplitQuery()
            .Include(x => x.ApplicantCompany)
            .Include(x => x.Application)
            .Include(x => x.ApprovalDetail)
            .Include(x => x.Documents)
            .Include(x => x.Installments)
            .Include(x => x.Payments)
            .Include(x => x.Comments)
            .Include(x => x.WorkflowHistory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<LoanCase?> GetScopedAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken)
        => ApplyScopedFilter(
                dbContext.LoanCases
                    .AsSplitQuery()
                    .Include(x => x.ApplicantCompany)
                    .Include(x => x.Application)
                    .Include(x => x.ApprovalDetail)
                    .Include(x => x.Documents)
                    .Include(x => x.Installments)
                    .Include(x => x.Payments)
                    .Include(x => x.Comments)
                    .Include(x => x.WorkflowHistory),
                userId,
                isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<LoanCase?> GetScopedForTransitionAsync(
        Guid id,
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
        => ApplyScopedFilter(
                dbContext.LoanCases
                    .AsSplitQuery()
                    .Include(x => x.Application)
                    .Include(x => x.ApprovalDetail)
                    .Include(x => x.Documents)
                    .Include(x => x.Installments)
                    .Include(x => x.Payments)
                    .Include(x => x.WorkflowHistory),
                userId,
                isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<string?> GetWorkflowInstanceIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.LoanCases
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.WorkflowInstanceId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<LoanCase?> GetScopedWithDocumentsAsync(
        Guid id,
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
        => ApplyScopedFilter(dbContext.LoanCases.Include(x => x.Documents), userId, isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<LoanCase?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken)
        => dbContext.LoanCases.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber, cancellationToken);

    public Task AddAsync(LoanCase loanCase, CancellationToken cancellationToken)
        => dbContext.LoanCases.AddAsync(loanCase, cancellationToken).AsTask();

    public Task<bool> ExistsCaseNumberAsync(string caseNumber, CancellationToken cancellationToken)
        => dbContext.LoanCases.AnyAsync(x => x.CaseNumber == caseNumber, cancellationToken);

    public async Task<IEnumerable<LoanCase>> SearchScopedAsync(
        string? caseNumber,
        string? applicantUserId,
        LoanCasePhase? phase,
        LoanCaseStatus? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
    {
        var query = dbContext.LoanCases
            .AsNoTracking()
            .Include(x => x.ApplicantCompany)
            .Include(x => x.Application)
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

    public async Task<IReadOnlyList<LoanKanbanCaseProjection>> ListActiveKanbanProjectionsAsync(
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
    {
        var terminalStatuses = new[]
        {
            LoanCaseStatus.Completed,
            LoanCaseStatus.CanceledByCeo,
            LoanCaseStatus.Archived
        };

        var query = dbContext.LoanCases
            .AsNoTracking()
            .Where(x => !terminalStatuses.Contains(x.CurrentStatus));

        if (!isInternalUser)
            query = query.Where(x => x.ApplicantUserId == userId);

        return await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new LoanKanbanCaseProjection(
                x.Id,
                x.CaseNumber,
                x.ApplicantType,
                x.CurrentPhase,
                x.CurrentStatus,
                x.CreatedAt,
                x.UpdatedAt,
                x.Application != null ? x.Application.RequestedAmount : null,
                x.ApplicantCompany != null ? x.ApplicantCompany.Name : null))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<LoanCase> ApplyScopedFilter(
        IQueryable<LoanCase> query,
        string userId,
        bool isInternalUser)
    {
        if (!isInternalUser)
            query = query.Where(x => x.ApplicantUserId == userId);

        return query;
    }
}

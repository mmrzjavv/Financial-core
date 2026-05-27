using Core.Application.Abstractions;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence;

public sealed class GuaranteeCaseRepository(CoreDbContext dbContext) : IGuaranteeCaseRepository
{
    public Task<GuaranteeCase?> GetAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.GuaranteeCases
            .AsSplitQuery()
            .Include(x => x.ApplicantCompany)
            .Include(x => x.Application)
            .Include(x => x.ApprovalForm)
            .Include(x => x.Documents)
            .Include(x => x.Comments)
            .Include(x => x.WorkflowHistory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<GuaranteeCase?> GetScopedAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken)
        => ApplyScopedFilter(
                dbContext.GuaranteeCases
                    .AsSplitQuery()
                    .Include(x => x.ApplicantCompany)
                    .Include(x => x.Application)
                    .Include(x => x.ApprovalForm)
                    .Include(x => x.Documents)
                    .Include(x => x.Comments)
                    .Include(x => x.WorkflowHistory),
                userId,
                isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<GuaranteeCase?> GetScopedForTransitionAsync(
        Guid id,
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
        => ApplyScopedFilter(
                dbContext.GuaranteeCases
                    .AsSplitQuery()
                    .Include(x => x.Application)
                    .Include(x => x.ApprovalForm)
                    .Include(x => x.Documents)
                    .Include(x => x.WorkflowHistory),
                userId,
                isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<string?> GetWorkflowInstanceIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.GuaranteeCases
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.WorkflowInstanceId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<GuaranteeCase?> GetScopedWithDocumentsAsync(
        Guid id,
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
        => ApplyScopedFilter(dbContext.GuaranteeCases.Include(x => x.Documents), userId, isInternalUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<GuaranteeCase?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken)
        => dbContext.GuaranteeCases.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber, cancellationToken);

    public Task AddAsync(GuaranteeCase guaranteeCase, CancellationToken cancellationToken)
        => dbContext.GuaranteeCases.AddAsync(guaranteeCase, cancellationToken).AsTask();

    public Task<bool> ExistsCaseNumberAsync(string caseNumber, CancellationToken cancellationToken)
        => dbContext.GuaranteeCases.AnyAsync(x => x.CaseNumber == caseNumber, cancellationToken);

    public async Task<IEnumerable<GuaranteeCase>> SearchScopedAsync(
        string? caseNumber,
        string? applicantUserId,
        GuaranteeCasePhase? phase,
        GuaranteeCaseStatus? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
    {
        var query = dbContext.GuaranteeCases
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

    public async Task<IReadOnlyList<GuaranteeKanbanCaseProjection>> ListActiveKanbanProjectionsAsync(
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken)
    {
        var terminalStatuses = new[]
        {
            GuaranteeCaseStatus.Completed,
            GuaranteeCaseStatus.Rejected,
            GuaranteeCaseStatus.Cancelled,
            GuaranteeCaseStatus.Archived
        };

        var query = dbContext.GuaranteeCases
            .AsNoTracking()
            .Where(x => !terminalStatuses.Contains(x.CurrentStatus));

        if (!isInternalUser)
            query = query.Where(x => x.ApplicantUserId == userId);

        return await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new GuaranteeKanbanCaseProjection(
                x.Id,
                x.CaseNumber,
                x.ApplicantType,
                x.CurrentPhase,
                x.CurrentStatus,
                x.CreatedAt,
                x.UpdatedAt,
                null,
                x.ApplicantCompany != null ? x.ApplicantCompany.Name : null))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<GuaranteeCase> ApplyScopedFilter(
        IQueryable<GuaranteeCase> query,
        string userId,
        bool isInternalUser)
    {
        if (!isInternalUser)
            query = query.Where(x => x.ApplicantUserId == userId);

        return query;
    }
}

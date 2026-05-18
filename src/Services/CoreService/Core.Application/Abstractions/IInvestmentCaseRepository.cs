using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.Abstractions;

public interface IInvestmentCaseRepository
{
    Task<InvestmentCase?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<InvestmentCase?> GetScopedAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);

    /// <summary>Scoped load for workflow transitions (minimal graph, avoids spurious parent updates).</summary>
    Task<InvestmentCase?> GetScopedForTransitionAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);

    Task<string?> GetWorkflowInstanceIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Scoped load with documents only (presign/upload/confirm).</summary>
    Task<InvestmentCase?> GetScopedWithDocumentsAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);
    Task<InvestmentCase?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken);
    Task AddAsync(InvestmentCase investmentCase, CancellationToken cancellationToken);
    Task<bool> ExistsCaseNumberAsync(string caseNumber, CancellationToken cancellationToken);
    Task<IEnumerable<InvestmentCase>> SearchAsync(
        string? caseNumber,
        string? applicantUserId,
        CasePhase? phase,
        CaseStatus? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IEnumerable<InvestmentCase>> SearchScopedAsync(
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
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KanbanCaseProjection>> ListActiveKanbanProjectionsAsync(
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken);
}
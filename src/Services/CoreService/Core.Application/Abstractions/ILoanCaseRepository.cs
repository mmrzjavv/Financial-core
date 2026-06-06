using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Abstractions;

public interface ILoanCaseRepository
{
    Task<LoanCase?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<LoanCase?> GetScopedAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);
    Task<LoanCase?> GetScopedForTransitionAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);
    Task<string?> GetWorkflowInstanceIdAsync(Guid id, CancellationToken cancellationToken);
    Task<LoanCase?> GetScopedWithDocumentsAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);
    Task<LoanCase?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken);
    Task AddAsync(LoanCase loanCase, CancellationToken cancellationToken);
    Task<bool> ExistsCaseNumberAsync(string caseNumber, CancellationToken cancellationToken);
    Task<IEnumerable<LoanCase>> SearchScopedAsync(
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
        CancellationToken cancellationToken);
    Task<IReadOnlyList<LoanKanbanCaseProjection>> ListActiveKanbanProjectionsAsync(
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken);
}

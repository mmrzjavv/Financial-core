using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Abstractions;

public interface IGuaranteeCaseRepository
{
    Task<GuaranteeCase?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GuaranteeCase?> GetScopedAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);
    Task<GuaranteeCase?> GetScopedForTransitionAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);
    Task<string?> GetWorkflowInstanceIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GuaranteeCase?> GetScopedWithDocumentsAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);
    Task<GuaranteeCase?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken);
    Task AddAsync(GuaranteeCase guaranteeCase, CancellationToken cancellationToken);
    Task<bool> ExistsCaseNumberAsync(string caseNumber, CancellationToken cancellationToken);
    Task<IEnumerable<GuaranteeCase>> SearchScopedAsync(
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
        CancellationToken cancellationToken);
    Task<IReadOnlyList<GuaranteeKanbanCaseProjection>> ListActiveKanbanProjectionsAsync(
        string userId,
        bool isInternalUser,
        CancellationToken cancellationToken);
}

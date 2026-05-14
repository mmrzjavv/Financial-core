using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.Abstractions;

public interface IInvestmentCaseRepository
{
    Task<InvestmentCase?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<InvestmentCase?> GetScopedAsync(Guid id, string userId, bool isInternalUser, CancellationToken cancellationToken);
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
}

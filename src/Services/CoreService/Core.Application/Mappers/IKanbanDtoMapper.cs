using Core.Application.Abstractions;
using Core.Application.DTOs;
using Core.Domain.Enums;

namespace Core.Application.Mappers;

public interface IKanbanDtoMapper
{
    KanbanCaseCardDto MapInvestmentActionCard(
        KanbanCaseProjection projection,
        string role,
        IReadOnlyList<string> allowedActions);

    KanbanCaseCardDto MapGuaranteeActionCard(
        GuaranteeKanbanCaseProjection projection,
        string role,
        IReadOnlyList<string> allowedActions);

    KanbanCaseCardDto MapRenewalActionCard(GuaranteeRenewalKanbanProjection projection);

    KanbanCaseCardDto MapLoanActionCard(
        LoanKanbanCaseProjection projection,
        string role,
        IReadOnlyList<string> allowedActions);

    KanbanCaseSummaryDto MapInvestmentWatchCard(KanbanCaseProjection projection, string role);

    KanbanCaseSummaryDto MapGuaranteeWatchCard(GuaranteeKanbanCaseProjection projection, string role);

    KanbanCaseSummaryDto MapRenewalWatchCard(GuaranteeRenewalKanbanProjection projection);

    KanbanCaseSummaryDto MapLoanWatchCard(LoanKanbanCaseProjection projection, string role);
}

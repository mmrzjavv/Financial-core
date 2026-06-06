using Core.Application.DTOs;
using Core.Domain.Entities;
using Core.Domain.Identity.Entities;

namespace Core.Application.Mappers;

public interface ILoanCaseDtoMapper
{
    LoanCaseDto MapCase(LoanCase entity, bool isInternalUser, Company? company, bool includeInstallments = false, bool includePayments = false);
    LoanApplicationDto MapApplication(LoanCaseApplication application);
    LoanApprovalDetailDto MapApprovalDetail(LoanApprovalDetail detail);
    LoanCaseDocumentDto MapDocument(LoanCaseDocument document);
    LoanInstallmentDto MapInstallment(LoanInstallment installment);
    LoanPaymentDto MapPayment(LoanPayment payment);
    LoanWorkflowHistoryDto MapHistory(LoanCaseWorkflowHistory history);
    LoanCaseCommentDto MapComment(LoanCaseComment comment);
}

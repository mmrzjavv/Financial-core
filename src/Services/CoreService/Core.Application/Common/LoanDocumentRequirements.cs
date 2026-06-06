using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Common;

public static class LoanDocumentRequirements
{
    public static readonly LoanDocumentType[] RequiredForDataEntrySubmit =
    [
        LoanDocumentType.SupplementaryAttachmentForm,
        LoanDocumentType.FundRequiredDocsAndCompanySpecs,
        LoanDocumentType.EstablishmentGazette,
        LoanDocumentType.CeoBoardIdCards,
        LoanDocumentType.FinancialAndCreditContracts,
        LoanDocumentType.FinancialStatements,
        LoanDocumentType.BankAccountTurnover,
        LoanDocumentType.MainLoanRequestForm,
        LoanDocumentType.ProposedFinancingContract
    ];

    public static IReadOnlyList<LoanDocumentType> GetMissingForDataEntrySubmit(IEnumerable<LoanCaseDocument> documents)
    {
        var present = documents
            .Where(d => !d.IsDeleted)
            .Select(d => d.DocumentType)
            .ToHashSet();

        return RequiredForDataEntrySubmit
            .Where(t => !present.Contains(t))
            .ToList();
    }

    public static string ToPersianLabel(LoanDocumentType type) => type switch
    {
        LoanDocumentType.SupplementaryAttachmentForm => "فرم پیوست الحاقی",
        LoanDocumentType.FundRequiredDocsAndCompanySpecs => "اسناد مورد نیاز صندوق و مشخصات شرکت",
        LoanDocumentType.EstablishmentGazette => "آگهی تأسیس و آخرین روزنامه رسمی",
        LoanDocumentType.CeoBoardIdCards => "کارت ملی و شناسنامه مدیرعامل و اعضای هیئت مدیره",
        LoanDocumentType.FinancialAndCreditContracts => "قراردادهای مالی و اعتباری",
        LoanDocumentType.FinancialStatements => "صورت‌های مالی",
        LoanDocumentType.BankAccountTurnover => "گردش/معدل حساب‌های بانکی",
        LoanDocumentType.MainLoanRequestForm => "فرم درخواست اصلی / نامه درخواست",
        LoanDocumentType.ProposedFinancingContract => "قرارداد پیشنهادی تأمین مالی",
        LoanDocumentType.EmployeeInsuranceReceipt => "آخرین رسید بیمه کارکنان",
        LoanDocumentType.ActivityLicenses => "تصویر مجوزهای فعالیت",
        LoanDocumentType.CreditInquiryResult => "نتیجه استعلام اعتبارسنجی",
        LoanDocumentType.RawContract => "قرارداد خام",
        LoanDocumentType.InstallmentScheduleExport => "خروجی جدول اقساط",
        LoanDocumentType.SignedContract => "قرارداد امضاشده",
        LoanDocumentType.SignedAttachment1 => "پیوست امضاشده ۱",
        LoanDocumentType.SignedAttachment2 => "پیوست امضاشده ۲",
        LoanDocumentType.SignedAttachment3 => "پیوست امضاشده ۳",
        LoanDocumentType.SignedAttachment4 => "پیوست امضاشده ۴",
        LoanDocumentType.SignedAttachment5 => "پیوست امضاشده ۵",
        LoanDocumentType.SignedAttachment6 => "پیوست امضاشده ۶",
        LoanDocumentType.FinalContract => "قرارداد نهایی",
        LoanDocumentType.PaymentReceipt => "رسید پرداخت",
        LoanDocumentType.Other => "سایر",
        _ => type.ToString()
    };

    public static string FormatDataEntryDocumentsIncompleteMessage(IEnumerable<LoanDocumentType> missing)
        => "مدارک الزامی هنوز کامل بارگذاری نشده است: "
           + string.Join("، ", missing.Select(ToPersianLabel));
}

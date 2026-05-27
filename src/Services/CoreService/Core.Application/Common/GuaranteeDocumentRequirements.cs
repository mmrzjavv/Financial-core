using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Common;

public static class GuaranteeDocumentRequirements
{
    public static readonly GuaranteeDocumentType[] RequiredForDataEntrySubmit =
    [
        GuaranteeDocumentType.EstablishmentGazette,
        GuaranteeDocumentType.FinancialStatements3Years,
        GuaranteeDocumentType.ActivityLicenses,
        GuaranteeDocumentType.BankAccountTurnover,
        GuaranteeDocumentType.CreditInformationForm,
        GuaranteeDocumentType.CaseFormationFeeReceipt,
        GuaranteeDocumentType.GuaranteeIssuanceRequestForm,
        GuaranteeDocumentType.CeoBoardIdCards
    ];

    public static IEnumerable<GuaranteeDocumentType> GetRequiredForDataEntrySubmit(GuaranteeType? guaranteeType)
    {
        foreach (var doc in RequiredForDataEntrySubmit)
            yield return doc;

        if (guaranteeType == GuaranteeType.Tender)
            yield return GuaranteeDocumentType.TenderAnnouncement;

        if (guaranteeType is GuaranteeType.PerformanceBond or GuaranteeType.AdvancePayment)
            yield return GuaranteeDocumentType.BaseContractImage;
    }

    public static IReadOnlyList<GuaranteeDocumentType> GetMissingForDataEntrySubmit(
        GuaranteeType? guaranteeType,
        IEnumerable<GuaranteeCaseDocument> documents)
    {
        var present = documents
            .Where(d => !d.IsDeleted)
            .Select(d => d.DocumentType)
            .ToHashSet();

        return GetRequiredForDataEntrySubmit(guaranteeType)
            .Where(t => !present.Contains(t))
            .ToList();
    }

    public static string ToPersianLabel(GuaranteeDocumentType type) => type switch
    {
        GuaranteeDocumentType.EstablishmentGazette => "آگهی تأسیس و آخرین روزنامه رسمی هیئت‌مدیره",
        GuaranteeDocumentType.FinancialStatements3Years => "صورت‌های مالی ۳ سال گذشته",
        GuaranteeDocumentType.ActivityLicenses => "تصویر مجوزهای اصلی فعالیت",
        GuaranteeDocumentType.BankAccountTurnover => "گردش حساب‌های بانکی فعال شرکت",
        GuaranteeDocumentType.CreditInformationForm => "فرم دریافت اطلاعات اعتباری",
        GuaranteeDocumentType.CaseFormationFeeReceipt => "فیش مبلغ تشکیل پرونده",
        GuaranteeDocumentType.GuaranteeIssuanceRequestForm => "فرم درخواست صدور ضمانت‌نامه",
        GuaranteeDocumentType.CeoBoardIdCards => "کارت ملی و شناسنامه مدیرعامل و اعضای هیئت‌مدیره",
        GuaranteeDocumentType.TenderAnnouncement => "تصویر آگهی مناقصه/مزایده",
        GuaranteeDocumentType.BaseContractImage => "تصویر قرارداد پایه",
        GuaranteeDocumentType.CaseFormationAttachmentForm => "فرم پیوست جهت تشکیل پرونده",
        GuaranteeDocumentType.CompanyIntroductionDocs => "اسناد معرفی شرکت",
        GuaranteeDocumentType.LeaseOrOwnershipDeed => "قرارداد اجاره یا سند مالکیت محل شرکت",
        GuaranteeDocumentType.FeasibilityReport => "گزارش امکان‌سنجی فنی و اقتصادی",
        GuaranteeDocumentType.SalesContractsScan => "اسکن قراردادهای فروش و تأییدیه‌ها",
        GuaranteeDocumentType.CreditInquiryResult => "استعلام اعتباری شرکت و مدیران",
        GuaranteeDocumentType.DraftContract => "پیش‌قرارداد (قرارداد خام)",
        GuaranteeDocumentType.SignedContract => "قرارداد امضاشده",
        GuaranteeDocumentType.SignedAttachment1 => "پیوست امضاشده ۱",
        GuaranteeDocumentType.SignedAttachment2 => "پیوست امضاشده ۲",
        GuaranteeDocumentType.SignedAttachment3 => "پیوست امضاشده ۳",
        GuaranteeDocumentType.SignedAttachment4 => "پیوست امضاشده ۴",
        GuaranteeDocumentType.SignedAttachment5 => "پیوست امضاشده ۵",
        GuaranteeDocumentType.SignedAttachment6 => "پیوست امضاشده ۶",
        GuaranteeDocumentType.FinalContract => "قرارداد نهایی",
        GuaranteeDocumentType.GuaranteeInstrument => "ضمانت‌نامه صادره",
        GuaranteeDocumentType.IssuanceReceipt => "رسید صدور",
        GuaranteeDocumentType.Other => "سایر",
        _ => type.ToString()
    };

    public static string FormatDataEntryDocumentsIncompleteMessage(IEnumerable<GuaranteeDocumentType> missing)
    {
        var labels = missing.Select(ToPersianLabel).ToList();
        if (labels.Count == 0)
            return ApiMessages.GuaranteeDocumentsIncomplete;

        return $"{ApiMessages.GuaranteeDocumentsIncomplete} موارد ناقص: {string.Join("، ", labels)}.";
    }

    public static readonly GuaranteeDocumentType[] RequiredForSignedPackageSubmit =
    [
        GuaranteeDocumentType.SignedContract
    ];

    public static readonly GuaranteeDocumentType[] RequiredForIssuance =
    [
        GuaranteeDocumentType.GuaranteeInstrument,
        GuaranteeDocumentType.IssuanceReceipt
    ];
}

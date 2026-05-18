namespace Core.Application.Notifications.Sms;

public static class SmsTemplateCatalog
{
    private static readonly Dictionary<SmsTemplateId, string> Templates = new()
    {
        [SmsTemplateId.CaseStatusChanged] =
            "پرونده {caseNumber}: وضعیت به «{statusTitle}» تغییر کرد.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.De1Submitted] =
            "پرونده {caseNumber}: فرم اولیه ثبت شد و در صف بررسی کارشناس قرار گرفت.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.De2Submitted] =
            "پرونده {caseNumber}: فرم تکمیلی ثبت شد و در صف بررسی کارشناس قرار گرفت.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.De1Approved] =
            "پرونده {caseNumber}: فرم اولیه تأیید شد. لطفاً فرم تکمیلی را تکمیل کنید.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.De1Revision] =
            "پرونده {caseNumber}: فرم اولیه نیاز به اصلاح دارد. وضعیت: «{statusTitle}».\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.De1Rejected] =
            "پرونده {caseNumber}: درخواست شما در مرحله فرم اولیه رد شد.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.De2Approved] =
            "پرونده {caseNumber}: فرم تکمیلی تأیید شد. پرونده وارد مرحله ارزش‌گذاری شد.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.De2Revision] =
            "پرونده {caseNumber}: فرم تکمیلی نیاز به اصلاح دارد. وضعیت: «{statusTitle}».\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.De2Rejected] =
            "پرونده {caseNumber}: درخواست شما در مرحله فرم تکمیلی رد شد.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.ValuationApproved] =
            "پرونده {caseNumber}: ارزش‌گذاری تأیید شد. وضعیت: «{statusTitle}».\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.ContractReady] =
            "پرونده {caseNumber}: پیش‌قرارداد آماده است. وضعیت: «{statusTitle}».\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.FinancialWorksheetApproved] =
            "پرونده {caseNumber}: کاربرگ مالی تأیید شد و برای تأیید مدیرعامل ارسال شد.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.CeoApproved] =
            "پرونده {caseNumber}: تأیید مدیرعامل انجام شد. وضعیت: «{statusTitle}».\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.PaymentRecorded] =
            "پرونده {caseNumber}: پرداخت ثبت شد. وضعیت: «{statusTitle}».\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.CaseRejected] =
            "پرونده {caseNumber}: درخواست شما رد شد.\nصندوق پژوهش و فناوری غیردولتی مسکن",
        [SmsTemplateId.CaseCompleted] =
            "پرونده {caseNumber}: پرونده با موفقیت تکمیل شد.\nصندوق پژوهش و فناوری غیردولتی مسکن"
    };

    public static string Render(SmsTemplateId templateId, IReadOnlyDictionary<string, string>? args)
    {
        if (!Templates.TryGetValue(templateId, out var template))
            throw new ArgumentOutOfRangeException(nameof(templateId), templateId, "Unknown SMS template.");

        if (args is null || args.Count == 0)
            return template;

        var message = template;
        foreach (var (key, value) in args)
            message = message.Replace("{" + key + "}", value ?? string.Empty, StringComparison.Ordinal);

        return message;
    }
}

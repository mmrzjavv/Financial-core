namespace BuildingBlocks.Application.Common;

public static class SystemMessages
{
    public const string PostgresConnectionMissing = "رشته اتصال پایگاه داده Postgres تنظیم نشده است.";
    public const string JwtKeyMissing = "کلید JWT در پیکربندی تنظیم نشده است.";
    public const string EncKeyMissing = "کلید رمزنگاری در پیکربندی تنظیم نشده است.";
    public const string SmsApiKeyMissing = "کلید API پیامک در پیکربندی تنظیم نشده است.";
    public const string SmsSenderMissing = "شماره ارسال پیامک در پیکربندی تنظیم نشده است.";
    public const string DeserializationReturnedNull = "داده دریافتی قابل پردازش نیست.";
}

namespace Core.Application.Common;

public sealed class LoanSettingsOptions
{
    public const string SectionName = "LoanSettings";

    public int InstallmentReminderDays { get; set; } = 7;
    public int ReminderPollIntervalMinutes { get; set; } = 60;
}

namespace Core.Application.Notifications.Sms;

public interface ISmsDispatcher
{
    Task<bool> SendImmediateAsync(
        SmsTemplateId templateId,
        string mobile,
        IReadOnlyDictionary<string, string>? args,
        CancellationToken cancellationToken = default);

    Task EnqueueAsync(
        SmsTemplateId templateId,
        string mobile,
        IReadOnlyDictionary<string, string>? args,
        TimeSpan? delay = null,
        CancellationToken cancellationToken = default);
}

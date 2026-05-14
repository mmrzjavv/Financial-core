namespace Core.Application.Identity.Notifications;

public interface INotificationService
{
    Task SendOtpNotificationAsync(string mobileNumber, string otpCode, DateTime validTime, CancellationToken cancellationToken = default);
    Task SendSmsNotificationAsync(string mobileNumber, int messageId, CancellationToken cancellationToken = default);
    Task SendBulkSmsNotificationAsync(List<string> mobileNumbers, int messageId, CancellationToken cancellationToken = default);
}

public interface ISmsNotificationService : INotificationService
{
    // Direct SMS implementation
}

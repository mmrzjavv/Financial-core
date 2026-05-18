namespace Core.Application.Identity.Interfaces;

public interface ISmsService
{
    Task<bool> SendOtpAsync(string mobileNumber, string otpCode, DateTime validTime);
    Task<bool> SendSmsAsync(string mobileNumber, int messageId);
    Task<bool> SendBulkSmsAsync(List<string> mobileNumbers, int messageId);
    Task<bool> SendRawMessageAsync(string mobileNumber, string messageText);
}
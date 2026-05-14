using System.Text.Json;
using Core.Application.Identity.Common.Options;
using Core.Application.Identity.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;

namespace Core.Application.Identity.Services;

public class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string _apiKey;
    private readonly string _senderNumber;


    private readonly Dictionary<int, string> _smsMessages = new()
    {
        { 1, "ò«—»— ê—«„Ì° »Â ”«„«‰Â „« ŒÊ‘ ¬„œÌœ." },
        { 2, "”›«—‘ ‘„« »« „Ê›ﬁÌ  À»  Ê œ— Õ«· Å—œ«“‘ «” ." },
        { 3, "’Ê— Õ”«» ÃœÌœÌ »—«Ì ‘„« ’«œ— ‘œÂ «” . ÃÂ  Å—œ«Œ  »Â Å‰· „—«Ã⁄Â ò‰Ìœ." },
        { 4, "œ—ŒÊ«”  ‘„« »« „Ê›ﬁÌ  «‰Ã«„ ‘œ." }
    };

    public SmsService(HttpClient httpClient, IOptions<SmsOptions> options)
    {
        _httpClient = httpClient;
        _apiBaseUrl = string.IsNullOrWhiteSpace(options.Value.ApiBaseUrl)
            ? "https://api.kavenegar.com"
            : options.Value.ApiBaseUrl.TrimEnd('/');
        _apiKey = string.IsNullOrWhiteSpace(options.Value.ApiKey)
            ? throw new InvalidOperationException("Sms:ApiKey is missing from configuration.")
            : options.Value.ApiKey;
        _senderNumber = string.IsNullOrWhiteSpace(options.Value.SenderNumber)
            ? throw new InvalidOperationException("Sms:SenderNumber is missing from configuration.")
            : options.Value.SenderNumber;
    }

    public async Task<bool> SendOtpAsync(string mobileNumber, string otpCode, DateTime validTime)
    {
        try
        {
            var apiUrl = $"{_apiBaseUrl}/v1/{_apiKey}/sms/send.json";

            var messageText = BuildOtpMessage(otpCode, validTime);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("receptor", mobileNumber),
                new KeyValuePair<string, string>("sender", _senderNumber),
                new KeyValuePair<string, string>("message", messageText),
            });

            var response = await _httpClient.PostAsync(apiUrl, formContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Log.Warning("Kavenegar SMS error. StatusCode={StatusCode} Body={Body}", (int)response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception while sending OTP");
            return false;
        }
    }


    private string BuildOtpMessage(string otpCode, DateTime validUntil)
    {
        var validMinutes = (int)(validUntil - DateTime.UtcNow).TotalMinutes;

        return
         
            $"òœ  √ÌÌœ Ê—Êœ ‘„«: {otpCode}\n\n" +
            $"«Ì‰ òœ »Â „œ  {validMinutes} œﬁÌﬁÂ „⁄ »— «” .\n" +
            $"·ÿ›« «“ œ— «Œ Ì«— ﬁ—«— œ«œ‰ ¬‰ »Â œÌê—«‰ ŒÊœœ«—Ì ò‰Ìœ.\n\n" +
            $"’‰œÊﬁ ÅéÊÂ‘ Ê ›‰«Ê—Ì €Ì— œÊ· Ì „”ò‰";
    }


    public async Task<bool> SendSmsAsync(string mobileNumber, int messageId)
    {
        try
        {
            if (!_smsMessages.TryGetValue(messageId, out var messageText))
            {
                throw new ArgumentException("‘‰«”Â ÅÌ«„ò Ê«—œ ‘œÂ œ— œÌò‘‰—Ì „ÊÃÊœ ‰Ì” .");
            }

            string url = $"{_apiBaseUrl}/v1/{_apiKey}/sms/send.json";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("receptor", mobileNumber),
                new KeyValuePair<string, string>("sender", _senderNumber),
                new KeyValuePair<string, string>("message", messageText)
            });

            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending Normal SMS");
            return false;
        }
    }


    public async Task<bool> SendBulkSmsAsync(List<string> mobileNumbers, int messageId)
    {
        try
        {
            if (!_smsMessages.TryGetValue(messageId, out var messageText))
            {
                throw new ArgumentException("‘‰«”Â ÅÌ«„ò Ê«—œ ‘œÂ œ— œÌò‘‰—Ì „ÊÃÊœ ‰Ì” .");
            }


            var receptorsJson = JsonSerializer.Serialize(mobileNumbers);
            var sendersJson = JsonSerializer.Serialize(Enumerable.Repeat(_senderNumber, mobileNumbers.Count));
            var messagesJson = JsonSerializer.Serialize(Enumerable.Repeat(messageText, mobileNumbers.Count));

            string url = $"{_apiBaseUrl}/v1/{_apiKey}/sms/sendarray.json";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("receptor", receptorsJson),
                new KeyValuePair<string, string>("sender", sendersJson),
                new KeyValuePair<string, string>("message", messagesJson)
            });

            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending Bulk SMS");
            return false;
        }
    }
}

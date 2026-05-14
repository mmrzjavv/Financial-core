namespace Core.Application.Responses;

public sealed record PresignDownloadResponse(string Url, DateTimeOffset ExpiresAtUtc, string FileName);

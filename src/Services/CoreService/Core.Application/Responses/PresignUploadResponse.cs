namespace Core.Application.Responses;

public sealed record PresignUploadResponse(string S3Key, string Url, DateTimeOffset ExpiresAtUtc, int Version);


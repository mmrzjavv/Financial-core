namespace Core.Application.Responses;

public sealed record PresignGuaranteeUploadResponse(string S3Key, string Url, DateTimeOffset ExpiresAt, int Version);

namespace Services.CoreService.Core.Application.Contracts.Documents;

public sealed record PresignedUrlResponse(
    string UploadUrl,
    string S3Key,
    int Version);


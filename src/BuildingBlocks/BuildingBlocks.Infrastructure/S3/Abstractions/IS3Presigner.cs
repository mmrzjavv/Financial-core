namespace BuildingBlocks.Infrastructure.S3.Abstractions;

public interface IS3Presigner
{
    Task<PresignedUpload> GetPresignedUploadAsync(string s3Key, string mimeType, TimeSpan expiresIn, CancellationToken cancellationToken);
    Task<PresignedDownload> GetPresignedDownloadAsync(string s3Key, TimeSpan expiresIn, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken);
}

public sealed record PresignedUpload(string Url, DateTimeOffset ExpiresAtUtc);
public sealed record PresignedDownload(string Url, DateTimeOffset ExpiresAtUtc);


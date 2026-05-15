namespace Core.Application.Abstractions;

public interface IDocumentStorage
{
    Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignUploadAsync(string s3Key, string mimeType, TimeSpan expiresIn, CancellationToken cancellationToken);
    Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignDownloadAsync(string s3Key, TimeSpan expiresIn, CancellationToken cancellationToken);
    Task UploadAsync(string s3Key, Stream content, string mimeType, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken);
}


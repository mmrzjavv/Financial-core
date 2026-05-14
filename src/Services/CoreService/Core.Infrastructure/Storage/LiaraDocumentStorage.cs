using Core.Application.Abstractions;

namespace Core.Infrastructure.Storage;

public sealed class LiaraDocumentStorage(ILiaraObjectStorage objectStorage) : IDocumentStorage
{
    public Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignUploadAsync(string s3Key, string mimeType, TimeSpan expiresIn, CancellationToken cancellationToken)
        => objectStorage.PresignPutAsync(s3Key, mimeType, expiresIn, cancellationToken);

    public Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignDownloadAsync(string s3Key, TimeSpan expiresIn, CancellationToken cancellationToken)
        => objectStorage.PresignGetAsync(s3Key, expiresIn, cancellationToken);

    public Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken)
        => objectStorage.ExistsAsync(s3Key, cancellationToken);
}


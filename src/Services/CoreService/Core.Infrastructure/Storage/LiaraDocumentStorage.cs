using Core.Application.Abstractions;

namespace Core.Infrastructure.Storage;

public sealed class LiaraDocumentStorage(ILiaraObjectStorage objectStorage) : IDocumentStorage
{
    public Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignUploadAsync(string s3Key, string mimeType, TimeSpan expiresIn, CancellationToken cancellationToken)
        => objectStorage.PresignPutAsync(s3Key, mimeType, expiresIn, cancellationToken);

    public Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignDownloadAsync(string s3Key, TimeSpan expiresIn, CancellationToken cancellationToken)
        => objectStorage.PresignGetAsync(s3Key, expiresIn, cancellationToken);

    public Task UploadAsync(string s3Key, Stream content, string mimeType, CancellationToken cancellationToken)
        => objectStorage.UploadAsync(s3Key, content, mimeType, cancellationToken);

    public async Task<DocumentObjectMetadata?> GetMetadataAsync(string s3Key, CancellationToken cancellationToken)
    {
        var metadata = await objectStorage.GetObjectMetadataAsync(s3Key, cancellationToken);
        return metadata is null
            ? null
            : new DocumentObjectMetadata(metadata.ContentLength, metadata.ContentType);
    }

    public Task<Stream> OpenReadAsync(string s3Key, CancellationToken cancellationToken)
        => objectStorage.OpenReadAsync(s3Key, cancellationToken);

    public Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken)
        => objectStorage.ExistsAsync(s3Key, cancellationToken);
}


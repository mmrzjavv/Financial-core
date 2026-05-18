using BuildingBlocks.Infrastructure.S3.Abstractions;
using Core.Application.Abstractions;

namespace Core.Infrastructure.Storage;

public sealed class S3DocumentStorage(IS3Presigner presigner) : IDocumentStorage
{
    public async Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignUploadAsync(string s3Key, string mimeType, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var presigned = await presigner.GetPresignedUploadAsync(s3Key, mimeType, expiresIn, cancellationToken);
        return (presigned.Url, presigned.ExpiresAtUtc);
    }

    public async Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignDownloadAsync(string s3Key, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var presigned = await presigner.GetPresignedDownloadAsync(s3Key, expiresIn, cancellationToken);
        return (presigned.Url, presigned.ExpiresAtUtc);
    }

    public Task UploadAsync(string s3Key, Stream content, string mimeType, CancellationToken cancellationToken)
        => throw new NotSupportedException("Server-side document upload requires Liara object storage.");

    public Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken)
        => presigner.ExistsAsync(s3Key, cancellationToken);

    public Task<DocumentObjectMetadata?> GetMetadataAsync(string s3Key, CancellationToken cancellationToken)
        => throw new NotSupportedException("Object metadata requires Liara object storage.");

    public Task<Stream> OpenReadAsync(string s3Key, CancellationToken cancellationToken)
        => throw new NotSupportedException("Streaming download requires Liara object storage.");
}


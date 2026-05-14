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

    public Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken)
        => presigner.ExistsAsync(s3Key, cancellationToken);
}


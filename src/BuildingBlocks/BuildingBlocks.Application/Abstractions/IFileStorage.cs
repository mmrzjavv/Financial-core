namespace BuildingBlocks.Application.Abstractions;

public interface IFileStorage
{
    Task<string> CreatePresignedUploadUrlAsync(
        string key,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken ct);

    Task<string> CreatePresignedDownloadUrlAsync(
        string key,
        TimeSpan expiresIn,
        CancellationToken ct);
}


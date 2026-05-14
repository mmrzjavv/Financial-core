namespace Core.Infrastructure.Storage;

public interface ILiaraObjectStorage
{
    Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignPutAsync(string key, string contentType, TimeSpan expiresIn, CancellationToken cancellationToken);
    Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignGetAsync(string key, TimeSpan expiresIn, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
    string GetPermanentUrl(string key);
}


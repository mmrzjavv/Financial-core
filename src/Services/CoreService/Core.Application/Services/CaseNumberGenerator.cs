using Core.Application.Abstractions;

namespace Core.Application.Services;

public sealed class CaseNumberGenerator : ICaseNumberGenerator
{
    public Task<string> GenerateAsync(CancellationToken cancellationToken)
    {
        var date = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return Task.FromResult($"IC-{date}-{random}");
    }
}

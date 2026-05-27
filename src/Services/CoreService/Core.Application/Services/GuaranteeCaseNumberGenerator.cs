using Core.Application.Abstractions;

namespace Core.Application.Services;

public sealed class GuaranteeCaseNumberGenerator : IGuaranteeCaseNumberGenerator
{
    public Task<string> GenerateGuaranteeCaseAsync(CancellationToken cancellationToken = default)
    {
        var date = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return Task.FromResult($"GC-{date}-{random}");
    }

    public Task<string> GenerateRenewalCaseAsync(CancellationToken cancellationToken = default)
    {
        var date = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return Task.FromResult($"GR-{date}-{random}");
    }
}

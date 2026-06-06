using Core.Application.Abstractions;

namespace Core.Application.Services;

public sealed class LoanCaseNumberGenerator : ILoanCaseNumberGenerator
{
    public Task<string> GenerateLoanCaseAsync(CancellationToken cancellationToken = default)
    {
        var date = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return Task.FromResult($"LN-{date}-{random}");
    }
}

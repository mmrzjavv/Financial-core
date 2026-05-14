namespace Core.Application.Abstractions;

public interface ICaseNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken);
}


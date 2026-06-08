using Core.Application.DTOs;

namespace Core.Application.Abstractions;

public interface IUserDisplayLookup
{
    Task<IReadOnlyDictionary<string, UserDisplayDto>> GetByIdsAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);

    string? ResolveFullName(IReadOnlyDictionary<string, UserDisplayDto> lookup, string? userId);
}

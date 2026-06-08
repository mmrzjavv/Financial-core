using Core.Application.Abstractions;
using Core.Application.DTOs;
using Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Identity;

public sealed class UserDisplayLookup(CoreDbContext dbContext) : IUserDisplayLookup
{
    public async Task<IReadOnlyDictionary<string, UserDisplayDto>> GetByIdsAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var parsedIds = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .Select(id => Guid.TryParse(id, out var guid) ? (string?)id : null)
            .Where(id => id is not null)
            .Select(id => Guid.Parse(id!))
            .ToArray();

        if (parsedIds.Length == 0)
            return new Dictionary<string, UserDisplayDto>(StringComparer.Ordinal);

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => parsedIds.Contains(u.Id))
            .Select(u => new UserDisplayDto(
                u.Id.ToString(),
                (u.FirstName + " " + u.LastName).Trim(),
                u.PhoneNumber))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(x => x.UserId, StringComparer.Ordinal);
    }

    public string? ResolveFullName(IReadOnlyDictionary<string, UserDisplayDto> lookup, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return lookup.TryGetValue(userId.Trim(), out var display) ? display.FullName : null;
    }
}

using System.Security.Claims;
using BuildingBlocks.Domain.Abstractions;

namespace Core.API;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
{
    public string? UserId => accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? UserName => accessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
    public IReadOnlyCollection<string> Roles => accessor.HttpContext?.User?.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray()
                                                ?? Array.Empty<string>();
}

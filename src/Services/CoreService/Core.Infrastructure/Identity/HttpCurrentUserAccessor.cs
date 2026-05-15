using System.Security.Claims;
using Core.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Core.Infrastructure.Identity;

public sealed class HttpCurrentUserAccessor(IHttpContextAccessor accessor) : ICurrentUserAccessor
{
    public Guid? UserId
    {
        get
        {
            var raw = accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
        }
    }

    public Guid? SessionId
    {
        get
        {
            var raw = accessor.HttpContext?.User?.FindFirstValue("sid");
            return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
        }
    }
}

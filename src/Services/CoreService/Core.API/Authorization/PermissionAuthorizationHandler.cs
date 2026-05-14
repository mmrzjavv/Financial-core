using System.Security.Claims;
using Core.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Core.API.Authorization;

public sealed class PermissionAuthorizationHandler(IIdentityClient identityClient, ILogger<PermissionAuthorizationHandler> logger)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        try
        {
            var allowed = await identityClient.ValidateUserPermissionAsync(userId, requirement.Permission, CancellationToken.None);
            if (allowed)
                context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Permission validation via IdentityService failed.");
        }
    }
}


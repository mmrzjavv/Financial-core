using Microsoft.AspNetCore.Authorization;

namespace Core.API.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}


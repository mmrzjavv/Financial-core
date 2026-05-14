using BuildingBlocks.Application.Results;

namespace Core.Application.Authorization;

public interface ICaseAuthorizationService
{
    string? UserId { get; }
    bool IsInternalUser { get; }
    bool HasPermission(string permission);

    Result EnsureAuthenticated();
}

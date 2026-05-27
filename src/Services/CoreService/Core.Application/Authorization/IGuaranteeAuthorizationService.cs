using BuildingBlocks.Application.Results;

namespace Core.Application.Authorization;

public interface IGuaranteeAuthorizationService
{
    string? UserId { get; }
    bool IsInternalUser { get; }
    Result<string> EnsureAuthenticated();
    bool HasPermission(string permission);
}

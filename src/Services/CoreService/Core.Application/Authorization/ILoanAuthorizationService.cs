using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;

namespace Core.Application.Authorization;

public interface ILoanAuthorizationService
{
    string? UserId { get; }
    bool IsInternalUser { get; }
    Result<string> EnsureAuthenticated();
    bool HasPermission(string permission);
}

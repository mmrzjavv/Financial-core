using Core.Application.Identity.Abstractions;
using Core.Application.Identity.Authorization;
using Services.CoreService.Core.Domain.Identity.Entities;


namespace Core.Application.Identity.Services.Authorization;

public class AuthorizationService : IAuthorizationService
{
    private readonly IPermissionService _permissionService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthorizationService(
        IPermissionService permissionService,
        IUnitOfWork unitOfWork)
    {
        _permissionService = permissionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> AuthorizeAsync(User user, string permission, CancellationToken cancellationToken = default)
    {
        return await _permissionService.HasPermissionAsync(user, permission, cancellationToken);
    }

    public async Task<bool> AuthorizeAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetAsync(u => u.Id == userId);
        if (user == null)
        {
            return false;
        }

        return await _permissionService.HasPermissionAsync(user, permission, cancellationToken);
    }
}
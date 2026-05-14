using Core.Application.Abstractions;
using Core.Application.Identity.Abstractions;
using Core.Application.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Domain.Identity.Entities;
using Services.CoreService.Core.Persistence;
using IdentityAuthorizationService = Core.Application.Identity.Authorization.IAuthorizationService;

namespace Core.Infrastructure.Identity.Services;

public sealed class LocalIdentityClient(
    IUnitOfWork unitOfWork,
    IdentityAuthorizationService authorizationService,
    IPermissionService permissionService,
    CoreDbContext dbContext) : IIdentityClient
{
    public async Task<UserInfo?> GetUserByIdAsync(string userId, bool includePermissions, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var id))
            return null;

        var user = await unitOfWork.Users.GetByIdAsync(id, disableTracking: true);
        return user is null ? null : await MapUserAsync(user, includePermissions, cancellationToken);
    }

    public async Task<IReadOnlyList<UserInfo>> GetUsersByIdsAsync(IReadOnlyCollection<string> userIds, bool includePermissions, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return Array.Empty<UserInfo>();

        var ids = new List<Guid>(userIds.Count);
        foreach (var userId in userIds)
        {
            if (Guid.TryParse(userId, out var id))
                ids.Add(id);
        }

        if (ids.Count == 0)
            return Array.Empty<UserInfo>();

        var users = await unitOfWork.Users.GetAllAsync(user => ids.Contains(user.Id), disableTracking: true);
        var results = new List<UserInfo>(users.Count);
        foreach (var user in users)
        {
            var mapped = await MapUserAsync(user, includePermissions, cancellationToken);
            if (mapped is not null)
                results.Add(mapped);
        }

        return results;
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var id))
            return Array.Empty<string>();

        var user = await unitOfWork.Users.GetByIdAsync(id, disableTracking: true);
        return user is null ? Array.Empty<string>() : [user.Role.ToString()];
    }

    public async Task<bool> ValidateUserPermissionAsync(string userId, string permission, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var id))
            return false;

        return await authorizationService.AuthorizeAsync(id, permission, cancellationToken);
    }

    public async Task<ApplicantProfile?> GetApplicantProfileAsync(string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var id))
            return null;

        var user = await unitOfWork.Users.GetByIdAsync(id, disableTracking: true);
        if (user is null)
            return null;

        CompanyInfo? company = null;
        if (user.CompanyId is Guid companyId)
            company = await GetCompanyInfoAsync(companyId.ToString(), cancellationToken);

        return new ApplicantProfile(
            user.Id.ToString(),
            $"{user.FirstName} {user.LastName}".Trim(),
            user.PhoneNumber,
            user.ApplicantType.ToString(),
            company);
    }

    public async Task<CompanyInfo?> GetCompanyInfoAsync(string companyId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(companyId, out var id))
            return null;

        var company = await dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(company => company.Id == id, cancellationToken);

        return company is null
            ? null
            : new CompanyInfo(
                company.Id.ToString(),
                company.Name,
                company.RegistrationNumber ?? string.Empty,
                company.PhoneNumber ?? string.Empty,
                company.Address ?? string.Empty);
    }

    private async Task<UserInfo?> MapUserAsync(User user, bool includePermissions, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> permissions = Array.Empty<string>();
        if (includePermissions)
        {
            var userPermissions = await permissionService.GetUserPermissionsAsync(user, cancellationToken);
            permissions = userPermissions as IReadOnlyList<string> ?? userPermissions.ToArray();
        }

        CompanyInfo? company = null;
        if (user.CompanyId is Guid companyId)
            company = await GetCompanyInfoAsync(companyId.ToString(), cancellationToken);

        return new UserInfo(
            user.Id.ToString(),
            user.PhoneNumber,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Role.ToString(),
            (int)user.Role,
            user.IsActive,
            user.IsPhoneVerified,
            user.NationalCode ?? string.Empty,
            user.ApplicantType.ToString(),
            company,
            permissions);
    }
}

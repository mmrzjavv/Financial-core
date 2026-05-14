namespace Core.Application.Abstractions;

public interface IIdentityClient
{
    Task<UserInfo?> GetUserByIdAsync(string userId, bool includePermissions, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserInfo>> GetUsersByIdsAsync(IReadOnlyCollection<string> userIds, bool includePermissions, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken);
    Task<bool> ValidateUserPermissionAsync(string userId, string permission, CancellationToken cancellationToken);
    Task<ApplicantProfile?> GetApplicantProfileAsync(string userId, CancellationToken cancellationToken);
    Task<CompanyInfo?> GetCompanyInfoAsync(string companyId, CancellationToken cancellationToken);
}

public sealed record UserInfo(
    string UserId,
    string PhoneNumber,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string Role,
    int RoleNumber,
    bool IsActive,
    bool IsPhoneVerified,
    string NationalCode,
    string ApplicantType,
    CompanyInfo? Company,
    IReadOnlyList<string> Permissions);

public sealed record ApplicantProfile(string UserId, string FullName, string PhoneNumber, string ApplicantType, CompanyInfo? Company);

public sealed record CompanyInfo(string CompanyId, string Name, string RegistrationNumber, string PhoneNumber, string Address);

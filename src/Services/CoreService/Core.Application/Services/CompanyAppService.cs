using Core.Application.Abstractions;
using Core.Application.Common;
using Core.Application.Requests;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using MapsterMapper;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application.Services;

public interface ICompanyAppService
{
    Task<Result<IReadOnlyList<CompanyDto>>> GetMyCompaniesAsync(CancellationToken cancellationToken);
    Task<Result<CompanyDto>> CreateAsync(SaveCompanyRequest request, CancellationToken cancellationToken);
    Task<Result<CompanyDto>> UpdateAsync(Guid companyId, SaveCompanyRequest request, CancellationToken cancellationToken);
}

public sealed class CompanyAppService(
    ICoreUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUser,
    IMapper mapper) : ICompanyAppService
{
    public async Task<Result<IReadOnlyList<CompanyDto>>> GetMyCompaniesAsync(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<IReadOnlyList<CompanyDto>>.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

        var companies = await unitOfWork.Companies.GetOwnedByUserAsync(userId, cancellationToken);
        return Result<IReadOnlyList<CompanyDto>>.Ok(companies.Select(c => mapper.Map<CompanyDto>(c)).ToArray());
    }

    public async Task<Result<CompanyDto>> CreateAsync(SaveCompanyRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<CompanyDto>.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

        var company = new Company
        {
            OwnerUserId = userId,
            Name = request.Name.Trim(),
            EconomicCode = request.EconomicCode.Trim(),
            RegistrationNumber = request.RegistrationNumber,
            NationalId = request.NationalId,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode
        };

        await unitOfWork.Companies.AddAsync(company, cancellationToken);

        var user = await unitOfWork.Users.GetByIdAsync(userId, disableTracking: false);
        if (user is not null && user.CompanyId is null)
        {
            user.CompanyId = company.Id;
            await unitOfWork.Users.UpdateAsync(user);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CompanyDto>.Ok(mapper.Map<CompanyDto>(company));
    }

    public async Task<Result<CompanyDto>> UpdateAsync(Guid companyId, SaveCompanyRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<CompanyDto>.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

        var company = await unitOfWork.Companies.GetByIdAsync(companyId, asNoTracking: false, cancellationToken);
        if (company is null)
            return Result<CompanyDto>.Fail(Error.NotFound(ApiMessages.CompanyNotFound));

        if (company.OwnerUserId != userId)
            return Result<CompanyDto>.Fail(Error.Forbidden(ApiMessages.CompanyAccessDenied));

        company.Name = request.Name.Trim();
        company.EconomicCode = request.EconomicCode.Trim();
        company.RegistrationNumber = request.RegistrationNumber;
        company.NationalId = request.NationalId;
        company.PhoneNumber = request.PhoneNumber;
        company.Address = request.Address;
        company.City = request.City;
        company.Province = request.Province;
        company.PostalCode = request.PostalCode;

        unitOfWork.Companies.Update(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CompanyDto>.Ok(mapper.Map<CompanyDto>(company));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = currentUser.UserId ?? Guid.Empty;
        return userId != Guid.Empty;
    }
}

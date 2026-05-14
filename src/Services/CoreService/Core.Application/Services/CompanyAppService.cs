using Core.Application.Abstractions;
using Core.Application.Common;
using Core.Application.Requests;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application.Services;

public interface ICompanyAppService
{
    Task<Result<IReadOnlyList<CompanyDto>>> GetMyCompaniesAsync(CancellationToken cancellationToken);
    Task<Result<CompanyDto>> CreateAsync(SaveCompanyRequest request, CancellationToken cancellationToken);
    Task<Result<CompanyDto>> UpdateAsync(Guid companyId, SaveCompanyRequest request, CancellationToken cancellationToken);
}

public sealed class CompanyAppService(
    ICoreDbContext dbContext,
    IUserContext userContext) : ICompanyAppService
{
    public async Task<Result<IReadOnlyList<CompanyDto>>> GetMyCompaniesAsync(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<IReadOnlyList<CompanyDto>>.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

        var companies = await dbContext.Companies
            .AsNoTracking()
            .Where(company => company.OwnerUserId == userId)
            .OrderBy(company => company.Name)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CompanyDto>>.Ok(companies.Select(Map).ToArray());
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

        dbContext.Companies.Add(company);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is not null && user.CompanyId is null)
            user.CompanyId = company.Id;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<CompanyDto>.Ok(Map(company));
    }

    public async Task<Result<CompanyDto>> UpdateAsync(Guid companyId, SaveCompanyRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<CompanyDto>.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

        var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
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

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<CompanyDto>.Ok(Map(company));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        return Guid.TryParse(userContext.UserId, out userId);
    }

    private static CompanyDto Map(Company company) =>
        new(
            company.Id,
            company.Name,
            company.EconomicCode,
            company.RegistrationNumber,
            company.NationalId,
            company.PhoneNumber,
            company.Address,
            company.City,
            company.Province,
            company.PostalCode);
}

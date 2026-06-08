using Core.Application.Requests;
using Core.Domain.Identity.Entities;

namespace Core.Application.Mappers;

public interface ICompanyDtoMapper
{
    CompanyDto? Map(Company? company);

    CompanyDto? MapFlat(
        Guid? companyId,
        string? name,
        string? economicCode,
        string? registrationNumber,
        string? nationalId,
        string? phoneNumber,
        string? address,
        string? city,
        string? province,
        string? postalCode);
}

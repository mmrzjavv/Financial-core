using Core.Application.Abstractions.Persistence;
using Services.CoreService.Core.Domain.Identity.Entities;
using Services.CoreService.Core.Persistence;

namespace Core.Infrastructure.Persistence;

public sealed class CompanyRepository(CoreDbContext dbContext)
    : EfRepository<Company, Guid>(dbContext, dbContext.Companies), ICompanyRepository
{
    public Task<List<Company>> GetOwnedByUserAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => ListAsync(company => company.OwnerUserId == ownerUserId, asNoTracking: true, cancellationToken: cancellationToken);
}

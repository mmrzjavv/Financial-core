using Core.Application.Abstractions.Persistence;
using Core.Domain.Identity.Entities;
using Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence;

public sealed class CompanyRepository(CoreDbContext dbContext)
    : EfRepository<Company, Guid>(dbContext, dbContext.Companies), ICompanyRepository
{
    public Task<List<Company>> GetOwnedByUserAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => ListAsync(company => company.OwnerUserId == ownerUserId, asNoTracking: true, cancellationToken: cancellationToken);

    public async Task<bool> HasLinkedCasesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (await dbContext.InvestmentCases.AnyAsync(
                x => x.CompanyId == companyId && !x.IsDeleted,
                cancellationToken))
            return true;

        if (await dbContext.GuaranteeCases.AnyAsync(
                x => x.CompanyId == companyId && !x.IsDeleted,
                cancellationToken))
            return true;

        return await dbContext.LoanCases.AnyAsync(
            x => x.CompanyId == companyId && !x.IsDeleted,
            cancellationToken);
    }
}

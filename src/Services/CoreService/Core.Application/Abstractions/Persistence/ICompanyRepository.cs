using BuildingBlocks.Persistence.Abstractions;
using Core.Domain.Identity.Entities;

namespace Core.Application.Abstractions.Persistence;

public interface ICompanyRepository : IWriteRepository<Company, Guid>
{
    Task<List<Company>> GetOwnedByUserAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Company>> GetPagedListAsync(int take, int skip, CancellationToken cancellationToken = default);
    Task<int> CountAllAsync(CancellationToken cancellationToken = default);
    Task<bool> HasLinkedCasesAsync(Guid companyId, CancellationToken cancellationToken = default);
}

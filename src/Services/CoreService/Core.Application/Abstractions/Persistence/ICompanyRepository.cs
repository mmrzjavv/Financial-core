using BuildingBlocks.Persistence.Abstractions;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application.Abstractions.Persistence;

public interface ICompanyRepository : IWriteRepository<Company, Guid>
{
    Task<List<Company>> GetOwnedByUserAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
}

using BuildingBlocks.Persistence.Abstractions;
using Core.Persistence;
using Services.CoreService.Core.Persistence;

namespace Core.Infrastructure.Persistence;

public sealed class CoreUnitOfWork(CoreDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}


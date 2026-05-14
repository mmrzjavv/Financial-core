using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using BuildingBlocks.Persistence.Db.DomainEvents;
using Services.CoreService.Core.Persistence;

namespace Core.Persistence.DesignTime;

public sealed class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    public CoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=core;Username=postgres;Password=postgres");
        return new CoreDbContext(optionsBuilder.Options, new NoOpDomainEventDispatcher());
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IReadOnlyCollection<BuildingBlocks.Domain.Events.IDomainEvent> domainEvents, CancellationToken ct) => Task.CompletedTask;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Services.CoreService.Core.Domain.Entities;

namespace Services.CoreService.Core.Persistence.Interceptors;

/// <summary>
/// Never persist tracked UPDATEs on <see cref="InvestmentCase"/>; use ExecuteUpdate extensions instead.
/// Prevents DbUpdateConcurrencyException from legacy xmin/RowVersion on investment_cases.
/// </summary>
public sealed class InvestmentCaseUpdateSuppressorInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SuppressTrackedUpdates(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SuppressTrackedUpdates(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SuppressTrackedUpdates(DbContext? context)
    {
        if (context is null)
            return;

        foreach (var entry in context.ChangeTracker.Entries<InvestmentCase>()
                     .Where(e => e.State == EntityState.Modified)
                     .ToList())
        {
            entry.State = EntityState.Unchanged;
        }
    }
}

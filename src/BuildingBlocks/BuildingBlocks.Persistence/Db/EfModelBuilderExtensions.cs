using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BuildingBlocks.Persistence.Db;

public static class EfModelBuilderExtensions
{
    public static void ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null)
                continue;

            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(EfModelBuilderExtensions).GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var generic = method!.MakeGenericMethod(entityType.ClrType);
            generic.Invoke(null, [modelBuilder]);
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ISoftDelete
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    public static void SetUtcNowDefault(this IMutableProperty property)
    {
        property.SetDefaultValueSql("timezone('utc', now())");
    }
}

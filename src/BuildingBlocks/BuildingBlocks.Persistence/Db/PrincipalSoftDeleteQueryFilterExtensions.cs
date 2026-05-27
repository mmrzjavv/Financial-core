using System.Linq.Expressions;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace BuildingBlocks.Persistence.Db;

public static class PrincipalSoftDeleteQueryFilterExtensions
{
    public static void ApplyFilterForSoftDeletePrincipal<TDependent, TPrincipal>(
        this ModelBuilder modelBuilder,
        Expression<Func<TDependent, TPrincipal>> navigation)
        where TDependent : class
        where TPrincipal : class, ISoftDelete
    {
        var entityBuilder = modelBuilder.Entity<TDependent>();
        var parameter = Expression.Parameter(typeof(TDependent), "e");
        var navigationBody = ReplacingExpressionVisitor.Replace(
            navigation.Parameters[0],
            parameter,
            navigation.Body);
        var principalNotDeleted = Expression.Not(
            Expression.Property(navigationBody, nameof(ISoftDelete.IsDeleted)));

        var existingFilter = entityBuilder.Metadata.GetQueryFilter();
        Expression body = principalNotDeleted;
        if (existingFilter is not null)
        {
            var existingBody = ReplacingExpressionVisitor.Replace(
                existingFilter.Parameters[0],
                parameter,
                existingFilter.Body);
            body = Expression.AndAlso(existingBody, principalNotDeleted);
        }

        entityBuilder.HasQueryFilter(Expression.Lambda<Func<TDependent, bool>>(body, parameter));
    }
}

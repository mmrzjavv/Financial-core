using System.Linq.Expressions;
using BuildingBlocks.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence;

public abstract class EfRepository<TEntity, TKey>(DbContext dbContext, DbSet<TEntity> dbSet) : IWriteRepository<TEntity, TKey>
    where TEntity : class
{
    protected DbContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> DbSet { get; } = dbSet;

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id")!.Equals(id), cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        if (predicate is not null)
            query = query.Where(predicate);

        return await query.ToListAsync(cancellationToken);
    }

    public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(predicate, cancellationToken);

    public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null ? DbSet.CountAsync(cancellationToken) : DbSet.CountAsync(predicate, cancellationToken);

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual void Update(TEntity entity) => DbSet.Update(entity);

    public virtual void Remove(TEntity entity) => DbSet.Remove(entity);
}

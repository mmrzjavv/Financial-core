namespace BuildingBlocks.Persistence.Abstractions;

public interface IWriteRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class
{
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}

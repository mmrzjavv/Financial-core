using BuildingBlocks.Application.Results;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence.Queries;

public static class QueryablePagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, pageNumber, pageSize, total);
    }
}

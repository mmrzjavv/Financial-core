using System.Linq.Expressions;
using BuildingBlocks.Application.Queries;

namespace BuildingBlocks.Persistence.Queries;

public static class QueryableSortExtensions
{
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        string? sortBy,
        SortDirection direction,
        IReadOnlyDictionary<string, LambdaExpression> sortMap,
        Expression<Func<T, object>> defaultSort)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return ApplyDirection(source, defaultSort, direction);

        var key = sortBy.Trim();
        if (!sortMap.TryGetValue(key, out var mapped) && !sortMap.TryGetValue(key.ToLowerInvariant(), out mapped))
            return ApplyDirection(source, defaultSort, direction);

        return ApplyDirection(source, (Expression<Func<T, object>>)mapped, direction);
    }

    private static IQueryable<T> ApplyDirection<T>(
        IQueryable<T> source,
        Expression<Func<T, object>> keySelector,
        SortDirection direction)
        => direction == SortDirection.Desc
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}

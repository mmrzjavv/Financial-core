using System.Linq.Expressions;

namespace BuildingBlocks.Persistence.Queries;

public static class QueryableFilterExtensions
{
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, bool>> predicate)
        => condition ? source.Where(predicate) : source;

    public static IQueryable<T> WhereEqualsIf<T, TValue>(
        this IQueryable<T> source,
        TValue? value,
        Expression<Func<T, TValue>> selector)
        where TValue : struct
        => value.HasValue
            ? source.Where(BuildEqualsExpression(selector, value.Value))
            : source;

    public static IQueryable<T> WhereEqualsIf<T>(
        this IQueryable<T> source,
        string? value,
        Expression<Func<T, string>> selector)
        => !string.IsNullOrWhiteSpace(value)
            ? source.Where(BuildEqualsExpression(selector, value.Trim()))
            : source;

    public static IQueryable<T> WhereContainsIf<T>(
        this IQueryable<T> source,
        string? value,
        Expression<Func<T, string?>> selector)
        => !string.IsNullOrWhiteSpace(value)
            ? source.Where(BuildContainsExpression(selector, value.Trim()))
            : source;

    public static IQueryable<T> WhereRangeIf<T, TValue>(
        this IQueryable<T> source,
        TValue? min,
        TValue? max,
        Expression<Func<T, TValue>> selector)
        where TValue : struct, IComparable<TValue>
    {
        if (min.HasValue)
            source = source.Where(BuildGreaterOrEqualExpression(selector, min.Value));

        if (max.HasValue)
            source = source.Where(BuildLessOrEqualExpression(selector, max.Value));

        return source;
    }

    public static IQueryable<T> WhereFromIf<T>(
        this IQueryable<T> source,
        DateTimeOffset? value,
        Expression<Func<T, DateTimeOffset>> selector)
        => value.HasValue
            ? source.Where(BuildGreaterOrEqualExpression(selector, value.Value))
            : source;

    public static IQueryable<T> WhereToIf<T>(
        this IQueryable<T> source,
        DateTimeOffset? value,
        Expression<Func<T, DateTimeOffset>> selector)
        => value.HasValue
            ? source.Where(BuildLessOrEqualExpression(selector, value.Value))
            : source;

    private static Expression<Func<T, bool>> BuildEqualsExpression<T, TValue>(
        Expression<Func<T, TValue>> selector,
        TValue value)
    {
        var parameter = selector.Parameters[0];
        var body = Expression.Equal(selector.Body, Expression.Constant(value, typeof(TValue)));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression<Func<T, bool>> BuildContainsExpression<T>(
        Expression<Func<T, string?>> selector,
        string value)
    {
        var parameter = selector.Parameters[0];
        var notNull = Expression.NotEqual(selector.Body, Expression.Constant(null, typeof(string)));
        var contains = Expression.Call(
            typeof(string),
            nameof(string.Contains),
            Type.EmptyTypes,
            selector.Body!,
            Expression.Constant(value));
        var body = Expression.AndAlso(notNull, contains);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression<Func<T, bool>> BuildGreaterOrEqualExpression<T, TValue>(
        Expression<Func<T, TValue>> selector,
        TValue value)
        where TValue : struct
    {
        var parameter = selector.Parameters[0];
        var body = Expression.GreaterThanOrEqual(selector.Body, Expression.Constant(value, typeof(TValue)));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression<Func<T, bool>> BuildLessOrEqualExpression<T, TValue>(
        Expression<Func<T, TValue>> selector,
        TValue value)
        where TValue : struct
    {
        var parameter = selector.Parameters[0];
        var body = Expression.LessThanOrEqual(selector.Body, Expression.Constant(value, typeof(TValue)));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

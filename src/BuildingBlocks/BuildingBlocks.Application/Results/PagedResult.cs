namespace BuildingBlocks.Application.Results;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    public int PageNumber => Page;

    public int TotalPages => PageSize == 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);
}


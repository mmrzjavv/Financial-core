namespace BuildingBlocks.Contracts.Paging;

public sealed record PagedRequest(int Page = 1, int PageSize = 25, string? Sort = null)
{
    public int Page { get; init; } = Page < 1 ? 1 : Page;
    public int PageSize { get; init; } = PageSize switch
    {
        < 1 => 1,
        > 200 => 200,
        _ => PageSize
    };
}


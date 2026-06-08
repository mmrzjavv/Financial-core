namespace BuildingBlocks.Application.Queries;

/// <summary>
/// Base request for advanced list endpoints. Extend per module (Guarantee, Loan, Investment).
/// </summary>
public record PagedListRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    public int NormalizedPageNumber => PageNumber < 1 ? 1 : PageNumber;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => 1,
        > 200 => 200,
        _ => PageSize
    };
}

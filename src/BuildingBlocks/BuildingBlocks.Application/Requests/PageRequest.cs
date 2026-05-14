namespace BuildingBlocks.Application.Requests;

public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize is < 1 ? 20 : PageSize > 200 ? 200 : PageSize;
}


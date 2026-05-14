namespace BuildingBlocks.Application.Requests;

public sealed record SortRequest(string? SortBy = null, bool Desc = false);


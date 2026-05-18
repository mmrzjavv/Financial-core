namespace Core.Infrastructure.Storage;

public sealed record ObjectMetadata(long ContentLength, string? ContentType);

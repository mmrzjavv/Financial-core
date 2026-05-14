namespace BuildingBlocks.Infrastructure.S3;

public sealed class S3Options
{
    public string ServiceUrl { get; init; } = default!;
    public string AccessKey { get; init; } = default!;
    public string SecretKey { get; init; } = default!;
    public string BucketName { get; init; } = default!;
    public bool ForcePathStyle { get; init; } = true;
}


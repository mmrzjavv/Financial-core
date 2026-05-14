namespace Core.Infrastructure.Storage;

public sealed class LiaraStorageOptions
{
    public required string EndpointUrl { get; init; }
    public required string BucketName { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
}


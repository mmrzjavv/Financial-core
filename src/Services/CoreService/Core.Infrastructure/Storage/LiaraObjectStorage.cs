using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BuildingBlocks.Domain.Abstractions;
using Core.Infrastructure.Common;
using Microsoft.Extensions.Configuration;

namespace Core.Infrastructure.Storage;

public sealed class LiaraObjectStorage : ILiaraObjectStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _endpointUrl;
    private readonly IClock _clock;

    public LiaraObjectStorage(IConfiguration configuration, IClock clock)
    {
        _clock = clock;

        _bucketName = configuration["LiaraStorage:BucketName"]
                      ?? throw new InvalidOperationException(InfrastructureMessages.StorageBucketMissing);
        var accessKey = configuration["LiaraStorage:AccessKey"]
                        ?? throw new InvalidOperationException(InfrastructureMessages.StorageAccessKeyMissing);
        var secretKey = configuration["LiaraStorage:SecretKey"]
                        ?? throw new InvalidOperationException(InfrastructureMessages.StorageSecretKeyMissing);
        _endpointUrl = configuration["LiaraStorage:EndpointUrl"]
                       ?? throw new InvalidOperationException(InfrastructureMessages.StorageEndpointMissing);

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = _endpointUrl,
            ForcePathStyle = true
        };
        _s3Client = new AmazonS3Client(credentials, config);
    }

    public Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignPutAsync(string key, string contentType, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var expiresAt = _clock.UtcNow.Add(expiresIn);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult((url, expiresAt));
    }

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);
    }

    public Task<(string Url, DateTimeOffset ExpiresAtUtc)> PresignGetAsync(string key, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var expiresAt = _clock.UtcNow.Add(expiresIn);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime
        };
        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult((url, expiresAt));
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            }, cancellationToken);

            return response.HttpStatusCode is System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public string GetPermanentUrl(string key)
    {
        var endpointUri = new Uri(_endpointUrl);
        var host = endpointUri.Host;
        return $"https://{_bucketName}.{host}/{key}";
    }
}


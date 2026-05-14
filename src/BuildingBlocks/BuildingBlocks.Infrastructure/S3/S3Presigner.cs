using Amazon.S3;
using Amazon.S3.Model;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Infrastructure.S3.Abstractions;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.S3;

public sealed class S3Presigner(IOptions<S3Options> options, IClock clock) : IS3Presigner
{
    private readonly S3Options _options = options.Value;

    public Task<PresignedUpload> GetPresignedUploadAsync(string s3Key, string mimeType, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var expiresAt = clock.UtcNow.Add(expiresIn);
        var client = CreateClient(_options);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = mimeType
        };

        var url = client.GetPreSignedURL(request);
        return Task.FromResult(new PresignedUpload(url, expiresAt));
    }

    public Task<PresignedDownload> GetPresignedDownloadAsync(string s3Key, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var expiresAt = clock.UtcNow.Add(expiresIn);
        var client = CreateClient(_options);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = s3Key,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime
        };

        var url = client.GetPreSignedURL(request);
        return Task.FromResult(new PresignedDownload(url, expiresAt));
    }

    public async Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken)
    {
        var client = CreateClient(_options);
        try
        {
            var response = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = s3Key
            }, cancellationToken);

            return response.HttpStatusCode is System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private static AmazonS3Client CreateClient(S3Options options)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = options.ForcePathStyle
        };
        return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }
}

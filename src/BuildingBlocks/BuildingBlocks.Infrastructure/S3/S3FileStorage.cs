using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.S3;

public sealed class S3FileStorage : IFileStorage
{
    private readonly S3Options _options;
    private readonly IAmazonS3 _s3;

    public S3FileStorage(IOptions<S3Options> options)
    {
        _options = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = _options.ForcePathStyle,
            UseHttp = _options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
            AuthenticationRegion = RegionEndpoint.USEast1.SystemName
        };

        _s3 = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    public async Task<string> CreatePresignedUploadUrlAsync(string key, string contentType, TimeSpan expiresIn, CancellationToken ct)
    {
        await EnsureBucketExistsAsync(ct);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiresIn),
            ContentType = contentType
        };

        return _s3.GetPreSignedURL(request);
    }

    public async Task<string> CreatePresignedDownloadUrlAsync(string key, TimeSpan expiresIn, CancellationToken ct)
    {
        await EnsureBucketExistsAsync(ct);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn)
        };

        return _s3.GetPreSignedURL(request);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3, _options.BucketName);
        if (exists)
            return;

        await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _options.BucketName }, ct);
    }
}


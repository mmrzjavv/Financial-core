using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

var endpoint = "https://storage.c2.liara.site";
var bucket = "maskan-storages";
var key = "cases/IC-20260514-419A2C1A/PitchDeck/1.png";
var accessKey = "0404sclnsg5tn6oe";
var secretKey = "7d2f050d-d398-4242-8c80-2147d66c420c";

var config = new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" };
using var client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);

try {
    await client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = bucket, Key = key });
    Console.WriteLine("HEAD: OK");
} catch (Exception ex) { Console.WriteLine("HEAD: " + ex.Message); }

try {
    var list = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, Prefix = key, MaxKeys = 5 });
    Console.WriteLine("LIST count=" + list.S3Objects.Count);
    foreach (var o in list.S3Objects) Console.WriteLine("  key=" + o.Key);
} catch (Exception ex) { Console.WriteLine("LIST: " + ex.Message); }

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using DocVault.Core.Exceptions;
using DocVault.Core.ServiceAgents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocVault.Core.Infrastructure.Storage;

// RustFS ist S3-kompatibel, deshalb genuegt der normale AWS-S3-Client mit angepasster
// ServiceURL. ForcePathStyle ist noetig, weil RustFS Pfad-Stil-URLs erwartet (/bucket/key),
// nicht den Subdomain-Stil von echtem AWS S3.
public sealed class S3DocumentStorage : IDocumentStorage
{
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3DocumentStorage> _logger;
    private readonly AmazonS3Client _client;

    public S3DocumentStorage(IOptions<S3StorageOptions> options, ILogger<S3DocumentStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = true
        };
        var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        _client = new AmazonS3Client(credentials, config);
    }

    public async Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false
            };
            await _client.PutObjectAsync(request, ct);
            _logger.LogInformation("Uploaded object {ObjectKey} to bucket {Bucket}", objectKey, _options.BucketName);
        }
        catch (Exception ex) when (ex is AmazonS3Exception or HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to upload object {ObjectKey} to bucket {Bucket}", objectKey, _options.BucketName);
            throw new StorageException("The object storage is not reachable.", ex);
        }
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_options.BucketName, objectKey, ct);
            return response.ResponseStream;
        }
        catch (Exception ex) when (ex is AmazonS3Exception or HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to download object {ObjectKey} from bucket {Bucket}", objectKey, _options.BucketName);
            throw new StorageException("The object storage is not reachable.", ex);
        }
    }
}

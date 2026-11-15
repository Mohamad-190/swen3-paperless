namespace DocVault.Core.ServiceAgents;

public interface IDocumentStorage
{
    Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default);

    Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default);
}

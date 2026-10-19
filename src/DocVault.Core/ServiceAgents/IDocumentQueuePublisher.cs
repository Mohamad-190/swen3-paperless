namespace DocVault.Core.ServiceAgents;

public interface IDocumentQueuePublisher
{
    Task PublishDocumentUploadedAsync(DocumentUploadedMessage message, CancellationToken ct = default);
}

using System.Text.Json;
using DocVault.Core.ServiceAgents;

namespace DocVault.OcrWorker;

// "Leerer" OCR-Schritt (Sprint 3): Nachricht lesen und loggen.
// In Sprint 4 kommt hier das Laden aus MinIO und die OCR mit Tesseract dazu.
public sealed class OcrMessageHandler : IOcrMessageHandler
{
    // Gleiches Format wie beim Publisher in der WebApi (camelCase)
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<OcrMessageHandler> _logger;

    public OcrMessageHandler(ILogger<OcrMessageHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> HandleAsync(ReadOnlyMemory<byte> body, CancellationToken ct = default)
    {
        DocumentUploadedMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<DocumentUploadedMessage>(body.Span, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Received message is not valid JSON, discarding it");
            return Task.FromResult(false);
        }

        if (message is null || message.DocumentId == Guid.Empty)
        {
            _logger.LogWarning("Received message without document id, discarding it");
            return Task.FromResult(false);
        }

        _logger.LogInformation("Received document {DocumentId} ({FileName}, {ContentType}) for OCR",
            message.DocumentId, message.FileName, message.ContentType);

        return Task.FromResult(true);
    }
}

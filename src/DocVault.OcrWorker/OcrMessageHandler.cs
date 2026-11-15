using System.Text.Json;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using DocVault.Core.ServiceAgents;
using DocVault.OcrWorker.Ocr;
using Microsoft.Extensions.DependencyInjection;

namespace DocVault.OcrWorker;

// Ablauf pro Nachricht: Status -> Processing, Datei aus dem Object Storage holen -> OCR ->
// Text in Elasticsearch indexieren -> Status -> Completed (oder Failed bei einem Fehler).
public sealed class OcrMessageHandler : IOcrMessageHandler
{
    // Gleiches Format wie beim Publisher in der WebApi (camelCase)
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int LogPreviewLength = 80;

    private readonly IDocumentStorage _storage;
    private readonly IOcrEngine _ocrEngine;
    private readonly ISearchIndexer _searchIndexer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OcrMessageHandler> _logger;

    public OcrMessageHandler(
        IDocumentStorage storage,
        IOcrEngine ocrEngine,
        ISearchIndexer searchIndexer,
        IServiceScopeFactory scopeFactory,
        ILogger<OcrMessageHandler> logger)
    {
        _storage = storage;
        _ocrEngine = ocrEngine;
        _searchIndexer = searchIndexer;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(ReadOnlyMemory<byte> body, CancellationToken ct = default)
    {
        var message = Parse(body);
        if (message is null)
            return false;

        _logger.LogInformation("Received document {DocumentId} ({FileName}, {ContentType}) for OCR",
            message.DocumentId, message.FileName, message.ContentType);

        // Eigener DI-Scope pro Nachricht: der DbContext hinter IDocumentRepository ist
        // nicht fuer parallele/langlebige Nutzung gedacht (anders als die Singletons oben).
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        var document = await repository.GetByIdAsync(message.DocumentId, ct);
        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} no longer exists, discarding message", message.DocumentId);
            return false;
        }

        if (document.Status == DocumentStatus.Completed)
        {
            // Nachricht wurde erneut zugestellt (z. B. nach einem Absturz vor dem Ack) -
            // nicht nochmal verarbeiten, nur bestaetigen.
            _logger.LogInformation("Document {DocumentId} was already processed, skipping", message.DocumentId);
            return true;
        }

        document.Status = DocumentStatus.Processing;
        await repository.UpdateAsync(document, ct);

        try
        {
            string text;
            await using (var file = await _storage.DownloadAsync(message.ObjectKey, ct))
                text = await _ocrEngine.ExtractTextAsync(file, message.ContentType, ct);

            _logger.LogInformation("OCR for document {DocumentId} finished, {Length} characters recognized: {Preview}",
                message.DocumentId, text.Length, Preview(text));

            await _searchIndexer.IndexAsync(
                new DocumentSearchEntry(message.DocumentId, message.Title, message.FileName, text, message.UploadedAt), ct);

            document.Status = DocumentStatus.Completed;
            document.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(document, ct);

            return true;
        }
        catch (Exception ex) when (ex is StorageException or OcrException or SearchException)
        {
            _logger.LogError(ex, "Processing document {DocumentId} failed", message.DocumentId);

            document.Status = DocumentStatus.Failed;
            document.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(document, ct);

            return false;
        }
    }

    private DocumentUploadedMessage? Parse(ReadOnlyMemory<byte> body)
    {
        DocumentUploadedMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<DocumentUploadedMessage>(body.Span, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Received message is not valid JSON, discarding it");
            return null;
        }

        if (message is null || message.DocumentId == Guid.Empty)
        {
            _logger.LogWarning("Received message without document id, discarding it");
            return null;
        }

        if (string.IsNullOrWhiteSpace(message.ObjectKey))
        {
            _logger.LogWarning("Received message for document {DocumentId} without object key, discarding it",
                message.DocumentId);
            return null;
        }

        return message;
    }

    private static string Preview(string text)
    {
        var singleLine = text.ReplaceLineEndings(" ");
        return singleLine.Length <= LogPreviewLength ? singleLine : singleLine[..LogPreviewLength] + "...";
    }
}

using System.Text.Json;
using DocVault.Core.Exceptions;
using DocVault.Core.ServiceAgents;
using DocVault.OcrWorker.Ocr;

namespace DocVault.OcrWorker;

// Ablauf pro Nachricht: Datei aus dem Object Storage holen -> OCR -> Text in Elasticsearch indexieren.
public sealed class OcrMessageHandler : IOcrMessageHandler
{
    // Gleiches Format wie beim Publisher in der WebApi (camelCase)
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int LogPreviewLength = 80;

    private readonly IDocumentStorage _storage;
    private readonly IOcrEngine _ocrEngine;
    private readonly ISearchIndexer _searchIndexer;
    private readonly ILogger<OcrMessageHandler> _logger;

    public OcrMessageHandler(
        IDocumentStorage storage,
        IOcrEngine ocrEngine,
        ISearchIndexer searchIndexer,
        ILogger<OcrMessageHandler> logger)
    {
        _storage = storage;
        _ocrEngine = ocrEngine;
        _searchIndexer = searchIndexer;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(ReadOnlyMemory<byte> body, CancellationToken ct = default)
    {
        var message = Parse(body);
        if (message is null)
            return false;

        _logger.LogInformation("Received document {DocumentId} ({FileName}, {ContentType}) for OCR",
            message.DocumentId, message.FileName, message.ContentType);

        try
        {
            string text;
            await using (var file = await _storage.DownloadAsync(message.ObjectKey, ct))
                text = await _ocrEngine.ExtractTextAsync(file, message.ContentType, ct);

            _logger.LogInformation("OCR for document {DocumentId} finished, {Length} characters recognized: {Preview}",
                message.DocumentId, text.Length, Preview(text));

            await _searchIndexer.IndexAsync(
                new DocumentSearchEntry(message.DocumentId, message.Title, message.FileName, text, message.UploadedAt), ct);

            return true;
        }
        catch (Exception ex) when (ex is StorageException or OcrException or SearchException)
        {
            _logger.LogError(ex, "Processing document {DocumentId} failed", message.DocumentId);
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

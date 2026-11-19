using DocVault.Core.Exceptions;
using DocVault.Core.ServiceAgents;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocVault.Core.Infrastructure.Search;

// Singleton: der ElasticsearchClient ist thread-safe und soll wiederverwendet werden.
// Der Index wird beim ersten Schreiben automatisch angelegt (dynamisches Mapping reicht
// fuer Volltextsuche, Strings werden als "text" mit Standard-Analyzer indexiert).
public sealed class ElasticsearchIndexer : ISearchIndexer
{
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ElasticsearchIndexer> _logger;
    private readonly ElasticsearchClient _client;

    public ElasticsearchIndexer(IOptions<ElasticsearchOptions> options, ILogger<ElasticsearchIndexer> logger)
    {
        _options = options.Value;
        _logger = logger;

        var settings = new ElasticsearchClientSettings(new Uri(_options.Url))
            .DefaultIndex(_options.IndexName);
        _client = new ElasticsearchClient(settings);
    }

    public async Task IndexAsync(DocumentSearchEntry entry, CancellationToken ct = default)
    {
        IndexResponse response;
        try
        {
            response = await _client.IndexAsync(entry,
                i => i.Index(_options.IndexName).Id(entry.DocumentId.ToString()), ct);
        }
        catch (Exception ex) when (ex is TransportException or HttpRequestException)
        {
            _logger.LogError(ex, "Elasticsearch at {Url} is not reachable", _options.Url);
            throw new SearchException("The search index is not reachable.", ex);
        }

        if (!response.IsValidResponse)
        {
            _logger.LogError("Indexing document {DocumentId} into {Index} failed: {DebugInfo}",
                entry.DocumentId, _options.IndexName, response.DebugInformation);

            var reason = response.ElasticsearchServerError?.Error.Reason ?? "unknown error";
            throw response.TryGetOriginalException(out var original) && original is not null
                ? new SearchException($"Indexing failed: {reason}", original)
                : new SearchException($"Indexing failed: {reason}");
        }

        _logger.LogInformation("Indexed document {DocumentId} into {Index} ({Length} characters)",
            entry.DocumentId, _options.IndexName, entry.Content.Length);
    }

    public async Task<IReadOnlyList<Guid>> SearchAsync(string query, CancellationToken ct = default)
    {
        SearchResponse<DocumentSearchEntry> response;
        try
        {
            response = await _client.SearchAsync<DocumentSearchEntry>(s => s
                .Indices(_options.IndexName)
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Query(query)
                        .Fields(new[] { "content", "title", "fileName" })
                        .Fuzziness(new Fuzziness("AUTO")))), ct);
        }
        catch (Exception ex) when (ex is TransportException or HttpRequestException)
        {
            _logger.LogError(ex, "Elasticsearch at {Url} is not reachable", _options.Url);
            throw new SearchException("The search index is not reachable.", ex);
        }

        if (!response.IsValidResponse)
        {
            _logger.LogError("Searching {Index} for '{Query}' failed: {DebugInfo}",
                _options.IndexName, query, response.DebugInformation);

            var reason = response.ElasticsearchServerError?.Error.Reason ?? "unknown error";
            throw response.TryGetOriginalException(out var original) && original is not null
                ? new SearchException($"Search failed: {reason}", original)
                : new SearchException($"Search failed: {reason}");
        }

        return response.Documents.Select(d => d.DocumentId).ToList();
    }

    public async Task<string?> GetContentAsync(Guid documentId, CancellationToken ct = default)
    {
        GetResponse<DocumentSearchEntry> response;
        try
        {
            response = await _client.GetAsync<DocumentSearchEntry>(_options.IndexName, documentId.ToString(), ct);
        }
        catch (Exception ex) when (ex is TransportException or HttpRequestException)
        {
            _logger.LogError(ex, "Elasticsearch at {Url} is not reachable", _options.Url);
            throw new SearchException("The search index is not reachable.", ex);
        }

        // Noch nicht von der OCR verarbeitet (oder Index existiert noch nicht) -> kein Text.
        if (!response.IsValidResponse || !response.Found)
        {
            if (response.ApiCallDetails.HttpStatusCode is not (404 or null))
            {
                _logger.LogError("Reading document {DocumentId} from {Index} failed: {DebugInfo}",
                    documentId, _options.IndexName, response.DebugInformation);
                throw new SearchException("Reading the recognized text failed.");
            }
            return null;
        }

        return response.Source?.Content;
    }
}

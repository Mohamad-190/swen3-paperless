using System.Text;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using DocVault.Core.ServiceAgents;
using DocVault.OcrWorker;
using DocVault.OcrWorker.Ocr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DocVault.Tests.OcrWorker;

public class OcrMessageHandlerTests
{
    private readonly IDocumentStorage _storage = Substitute.For<IDocumentStorage>();
    private readonly IOcrEngine _ocrEngine = Substitute.For<IOcrEngine>();
    private readonly ISearchIndexer _searchIndexer = Substitute.For<ISearchIndexer>();
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly ListLogger<OcrMessageHandler> _logger = new();
    private readonly OcrMessageHandler _handler;

    private readonly Guid _documentId = Guid.NewGuid();
    private readonly DocumentEntity _document;

    public OcrMessageHandlerTests()
    {
        // Echter DI-Container statt Mock fuer IServiceScopeFactory: der Handler erzeugt
        // pro Nachricht einen eigenen Scope, in dem genau dieses _repository aufgeloest wird.
        var services = new ServiceCollection();
        services.AddSingleton(_repository);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _handler = new OcrMessageHandler(_storage, _ocrEngine, _searchIndexer, scopeFactory, _logger);

        _document = new DocumentEntity
        {
            Id = _documentId,
            Title = "Hello",
            FileName = "HelloWorld.pdf",
            ContentType = "application/pdf",
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow
        };
        _repository.GetByIdAsync(_documentId, Arg.Any<CancellationToken>()).Returns(_document);

        _storage.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes("%PDF-fake")));
        _ocrEngine.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Hello World");
    }

    private byte[] ValidMessage(string objectKey = "object-key-1") => Encoding.UTF8.GetBytes(
        $$"""{"documentId":"{{_documentId}}","title":"Hello","fileName":"HelloWorld.pdf","contentType":"application/pdf","objectKey":"{{objectKey}}","uploadedAt":"2026-11-02T10:00:00Z"}""");

    [Fact]
    public async Task HandleAsync_ValidMessage_DownloadsRunsOcrAndIndexesText()
    {
        var processed = await _handler.HandleAsync(ValidMessage());

        Assert.True(processed);
        await _storage.Received(1).DownloadAsync("object-key-1", Arg.Any<CancellationToken>());
        await _ocrEngine.Received(1).ExtractTextAsync(Arg.Any<Stream>(), "application/pdf", Arg.Any<CancellationToken>());
        await _searchIndexer.Received(1).IndexAsync(
            Arg.Is<DocumentSearchEntry>(e =>
                e.DocumentId == _documentId &&
                e.Title == "Hello" &&
                e.FileName == "HelloWorld.pdf" &&
                e.Content == "Hello World"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidMessage_SetsStatusToProcessingThenCompleted()
    {
        await _handler.HandleAsync(ValidMessage());

        Assert.Equal(DocumentStatus.Completed, _document.Status);
        await _repository.Received(2).UpdateAsync(_document, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyCompleted_SkipsReprocessing()
    {
        _document.Status = DocumentStatus.Completed;

        var processed = await _handler.HandleAsync(ValidMessage());

        Assert.True(processed);
        await _storage.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_DocumentNoLongerExists_ReturnsFalse()
    {
        _repository.GetByIdAsync(_documentId, Arg.Any<CancellationToken>()).Returns((DocumentEntity?)null);

        var processed = await _handler.HandleAsync(ValidMessage());

        Assert.False(processed);
        await _storage.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_ValidMessage_LogsReceivedDocument()
    {
        await _handler.HandleAsync(ValidMessage());

        Assert.Contains(_logger.Entries, e =>
            e.Level == LogLevel.Information && e.Message.Contains(_documentId.ToString()) && e.Message.Contains("HelloWorld.pdf"));
    }

    [Fact]
    public async Task HandleAsync_InvalidJson_ReturnsFalse()
    {
        var processed = await _handler.HandleAsync(Encoding.UTF8.GetBytes("not json"));

        Assert.False(processed);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error);
        await _storage.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_MissingDocumentId_ReturnsFalse()
    {
        var processed = await _handler.HandleAsync(Encoding.UTF8.GetBytes("""{"fileName":"HelloWorld.pdf"}"""));

        Assert.False(processed);
    }

    [Fact]
    public async Task HandleAsync_MissingObjectKey_ReturnsFalseWithoutDownloading()
    {
        var processed = await _handler.HandleAsync(ValidMessage(objectKey: ""));

        Assert.False(processed);
        await _storage.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_StorageFails_ReturnsFalseAndMarksFailed()
    {
        _storage.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new StorageException("down", new Exception()));

        var processed = await _handler.HandleAsync(ValidMessage());

        Assert.False(processed);
        Assert.Equal(DocumentStatus.Failed, _document.Status);
        await _searchIndexer.DidNotReceiveWithAnyArgs().IndexAsync(default!, default);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task HandleAsync_OcrFails_ReturnsFalseAndMarksFailed()
    {
        _ocrEngine.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OcrException("tesseract failed"));

        var processed = await _handler.HandleAsync(ValidMessage());

        Assert.False(processed);
        Assert.Equal(DocumentStatus.Failed, _document.Status);
        await _searchIndexer.DidNotReceiveWithAnyArgs().IndexAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_IndexingFails_ReturnsFalseAndMarksFailed()
    {
        _searchIndexer.IndexAsync(Arg.Any<DocumentSearchEntry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SearchException("es down"));

        var processed = await _handler.HandleAsync(ValidMessage());

        Assert.False(processed);
        Assert.Equal(DocumentStatus.Failed, _document.Status);
    }

    // Merkt sich alle Log-Eintraege, damit der Test den Log-Eintrag pruefen kann.
    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}

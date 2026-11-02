using System.Text;
using DocVault.OcrWorker;
using Microsoft.Extensions.Logging;

namespace DocVault.Tests.OcrWorker;

public class OcrMessageHandlerTests
{
    private readonly ListLogger<OcrMessageHandler> _logger = new();
    private readonly OcrMessageHandler _handler;

    public OcrMessageHandlerTests()
    {
        _handler = new OcrMessageHandler(_logger);
    }

    [Fact]
    public async Task HandleAsync_ValidMessage_ReturnsTrueAndLogsDocument()
    {
        var documentId = Guid.NewGuid();
        var json = $$"""{"documentId":"{{documentId}}","fileName":"HelloWorld.pdf","contentType":"application/pdf","uploadedAt":"2026-11-02T10:00:00Z"}""";

        var processed = await _handler.HandleAsync(Encoding.UTF8.GetBytes(json));

        Assert.True(processed);
        Assert.Contains(_logger.Entries, e =>
            e.Level == LogLevel.Information && e.Message.Contains(documentId.ToString()) && e.Message.Contains("HelloWorld.pdf"));
    }

    [Fact]
    public async Task HandleAsync_InvalidJson_ReturnsFalse()
    {
        var processed = await _handler.HandleAsync(Encoding.UTF8.GetBytes("not json"));

        Assert.False(processed);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task HandleAsync_MissingDocumentId_ReturnsFalse()
    {
        var processed = await _handler.HandleAsync(Encoding.UTF8.GetBytes("""{"fileName":"HelloWorld.pdf"}"""));

        Assert.False(processed);
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

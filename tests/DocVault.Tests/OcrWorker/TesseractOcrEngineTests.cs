using System.Text;
using DocVault.OcrWorker.Ocr;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocVault.Tests.OcrWorker;

public class TesseractOcrEngineTests
{
    private readonly FakeCommandRunner _runner = new();
    private readonly TesseractOcrEngine _engine;

    public TesseractOcrEngineTests()
    {
        _engine = new TesseractOcrEngine(_runner, Options.Create(new OcrOptions { Languages = "deu+eng", Dpi = 300 }),
            NullLogger<TesseractOcrEngine>.Instance);
    }

    private static MemoryStream Content() => new(Encoding.UTF8.GetBytes("fake file"));

    [Fact]
    public async Task ExtractTextAsync_Pdf_RendersPagesAndRecognizesEachPage()
    {
        _runner.PagesToRender = 2;

        var text = await _engine.ExtractTextAsync(Content(), "application/pdf");

        Assert.Equal("gs", _runner.Calls[0].FileName);
        Assert.Contains("-r300", _runner.Calls[0].Arguments);
        Assert.Equal(2, _runner.Calls.Count(c => c.FileName == "tesseract"));
        Assert.Contains("page-001.png", text);
        Assert.Contains("page-002.png", text);
        // Seiten muessen in der richtigen Reihenfolge erkannt werden
        Assert.True(text.IndexOf("page-001.png", StringComparison.Ordinal) < text.IndexOf("page-002.png", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExtractTextAsync_Png_SkipsGhostscript()
    {
        var text = await _engine.ExtractTextAsync(Content(), "image/png");

        Assert.DoesNotContain(_runner.Calls, c => c.FileName == "gs");
        var tesseract = Assert.Single(_runner.Calls);
        Assert.Equal(new[] { "stdout", "-l", "deu+eng" }, tesseract.Arguments.Skip(1));
        Assert.Contains("input.png", text);
    }

    [Fact]
    public async Task ExtractTextAsync_UnsupportedContentType_Throws()
    {
        await Assert.ThrowsAsync<OcrException>(() => _engine.ExtractTextAsync(Content(), "text/plain"));
        Assert.Empty(_runner.Calls);
    }

    [Fact]
    public async Task ExtractTextAsync_GhostscriptFails_Throws()
    {
        _runner.ExitCodeFor["gs"] = 1;

        var ex = await Assert.ThrowsAsync<OcrException>(() => _engine.ExtractTextAsync(Content(), "application/pdf"));
        Assert.Contains("Ghostscript", ex.Message);
    }

    [Fact]
    public async Task ExtractTextAsync_GhostscriptRendersNoPages_Throws()
    {
        _runner.PagesToRender = 0;

        await Assert.ThrowsAsync<OcrException>(() => _engine.ExtractTextAsync(Content(), "application/pdf"));
    }

    [Fact]
    public async Task ExtractTextAsync_TesseractFails_Throws()
    {
        _runner.ExitCodeFor["tesseract"] = 1;

        var ex = await Assert.ThrowsAsync<OcrException>(() => _engine.ExtractTextAsync(Content(), "image/jpeg"));
        Assert.Contains("Tesseract", ex.Message);
    }

    [Fact]
    public async Task ExtractTextAsync_DeletesTemporaryFiles()
    {
        _runner.PagesToRender = 1;

        await _engine.ExtractTextAsync(Content(), "application/pdf");

        var inputPath = _runner.Calls[0].Arguments[^1];
        Assert.False(Directory.Exists(Path.GetDirectoryName(inputPath)));
    }

    // Simuliert gs (legt leere PNG-Dateien an) und tesseract (gibt den Bildnamen als "erkannten Text" zurueck).
    private sealed class FakeCommandRunner : ICommandRunner
    {
        public List<(string FileName, IReadOnlyList<string> Arguments)> Calls { get; } = new();
        public Dictionary<string, int> ExitCodeFor { get; } = new();
        public int PagesToRender { get; set; } = 1;

        public Task<CommandResult> RunAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken ct = default)
        {
            Calls.Add((fileName, arguments));
            var exitCode = ExitCodeFor.GetValueOrDefault(fileName);
            if (exitCode != 0)
                return Task.FromResult(new CommandResult(exitCode, "", "boom"));

            if (fileName == "gs")
            {
                var pattern = arguments.Single(a => a.StartsWith("-sOutputFile=")).Substring("-sOutputFile=".Length);
                for (var page = 1; page <= PagesToRender; page++)
                    File.WriteAllBytes(pattern.Replace("%03d", page.ToString("000")), Array.Empty<byte>());
                return Task.FromResult(new CommandResult(0, "", ""));
            }

            return Task.FromResult(new CommandResult(0, $"text of {Path.GetFileName(arguments[0])}\n", ""));
        }
    }
}

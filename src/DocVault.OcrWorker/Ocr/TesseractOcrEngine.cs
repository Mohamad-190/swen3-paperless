using System.Text;
using Microsoft.Extensions.Options;

namespace DocVault.OcrWorker.Ocr;

// PDF: Ghostscript rendert jede Seite als PNG, danach erkennt Tesseract den Text pro Seite.
// PNG/JPEG gehen direkt an Tesseract. Alles laeuft in einem eigenen Temp-Ordner,
// der danach wieder geloescht wird.
public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly ICommandRunner _runner;
    private readonly OcrOptions _options;
    private readonly ILogger<TesseractOcrEngine> _logger;

    public TesseractOcrEngine(ICommandRunner runner, IOptions<OcrOptions> options, ILogger<TesseractOcrEngine> logger)
    {
        _runner = runner;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(Stream content, string contentType, CancellationToken ct = default)
    {
        var extension = GetExtension(contentType);
        var workDir = Path.Combine(Path.GetTempPath(), $"ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);

        try
        {
            var inputPath = Path.Combine(workDir, $"input{extension}");
            await using (var file = File.Create(inputPath))
                await content.CopyToAsync(file, ct);

            var images = extension == ".pdf"
                ? await RenderPdfPagesAsync(inputPath, workDir, ct)
                : new[] { inputPath };

            var text = new StringBuilder();
            foreach (var image in images)
                text.AppendLine(await RecognizeAsync(image, ct));

            return text.ToString().Trim();
        }
        finally
        {
            TryDelete(workDir);
        }
    }

    private async Task<string[]> RenderPdfPagesAsync(string pdfPath, string workDir, CancellationToken ct)
    {
        var outputPattern = Path.Combine(workDir, "page-%03d.png");
        var result = await _runner.RunAsync(_options.GhostscriptPath, new[]
        {
            "-dNOPAUSE", "-dBATCH", "-dSAFER", "-dQUIET",
            "-sDEVICE=png16m",
            $"-r{_options.Dpi}",
            $"-sOutputFile={outputPattern}",
            pdfPath
        }, ct);

        if (result.ExitCode != 0)
            throw new OcrException($"Ghostscript failed with exit code {result.ExitCode}: {result.StandardError.Trim()}");

        var pages = Directory.GetFiles(workDir, "page-*.png").Order(StringComparer.Ordinal).ToArray();
        if (pages.Length == 0)
            throw new OcrException("Ghostscript did not render any pages.");

        _logger.LogInformation("Rendered {PageCount} page(s) for OCR", pages.Length);
        return pages;
    }

    private async Task<string> RecognizeAsync(string imagePath, CancellationToken ct)
    {
        // "stdout" als Ausgabe -> Tesseract schreibt den Text direkt auf die Konsole statt in eine Datei.
        var result = await _runner.RunAsync(_options.TesseractPath,
            new[] { imagePath, "stdout", "-l", _options.Languages }, ct);

        if (result.ExitCode != 0)
            throw new OcrException($"Tesseract failed with exit code {result.ExitCode}: {result.StandardError.Trim()}");

        return result.StandardOutput;
    }

    private static string GetExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "application/pdf" => ".pdf",
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        _ => throw new OcrException($"Content type '{contentType}' is not supported for OCR.")
    };

    private void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete temporary OCR directory {Directory}", directory);
        }
    }
}

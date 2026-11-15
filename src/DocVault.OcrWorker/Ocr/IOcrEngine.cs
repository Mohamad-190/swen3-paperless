namespace DocVault.OcrWorker.Ocr;

public interface IOcrEngine
{
    Task<string> ExtractTextAsync(Stream content, string contentType, CancellationToken ct = default);
}

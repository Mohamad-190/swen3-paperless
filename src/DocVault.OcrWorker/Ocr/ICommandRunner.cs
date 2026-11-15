namespace DocVault.OcrWorker.Ocr;

public record CommandResult(int ExitCode, string StandardOutput, string StandardError);

// Abstraktion ueber externe Prozesse (gs, tesseract), damit die OCR-Logik ohne
// installierte Tools getestet werden kann.
public interface ICommandRunner
{
    Task<CommandResult> RunAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken ct = default);
}

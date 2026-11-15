namespace DocVault.OcrWorker.Ocr;

// Fehler der OCR-Schicht (Ghostscript/Tesseract fehlt, Exit-Code != 0, Dateityp nicht unterstuetzt, ...).
public class OcrException : Exception
{
    public OcrException(string message)
        : base(message)
    {
    }

    public OcrException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

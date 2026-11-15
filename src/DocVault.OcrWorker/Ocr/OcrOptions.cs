namespace DocVault.OcrWorker.Ocr;

public class OcrOptions
{
    public const string SectionName = "Ocr";

    // Tesseract-Sprachpakete, mit "+" verbunden (muessen im Image installiert sein).
    public string Languages { get; set; } = "deu+eng";

    // Aufloesung, mit der Ghostscript die PDF-Seiten rendert. 300 DPI ist der uebliche Wert fuer OCR.
    public int Dpi { get; set; } = 300;

    public string GhostscriptPath { get; set; } = "gs";
    public string TesseractPath { get; set; } = "tesseract";
}

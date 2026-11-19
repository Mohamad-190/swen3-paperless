namespace DocVault.Core.Dtos;

// Der per OCR erkannte Text eines Dokuments. Content ist null, solange noch nichts erkannt wurde.
public class DocumentContentDto
{
    public string? Content { get; set; }
}

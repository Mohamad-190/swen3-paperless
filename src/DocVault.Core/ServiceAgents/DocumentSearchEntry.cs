namespace DocVault.Core.ServiceAgents;

// Ein Eintrag im Suchindex: Metadaten + der per OCR erkannte Text.
public record DocumentSearchEntry(
    Guid DocumentId,
    string Title,
    string FileName,
    string Content,
    DateTime UploadedAt);

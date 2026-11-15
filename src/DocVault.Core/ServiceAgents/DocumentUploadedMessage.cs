namespace DocVault.Core.ServiceAgents;

// Nachricht an die OCR-Queue, wird vom OCR-Worker konsumiert.
// ObjectKey = Name der Datei im Object Storage, damit der Worker sie dort abholen kann.
public record DocumentUploadedMessage(
    Guid DocumentId,
    string Title,
    string FileName,
    string ContentType,
    string ObjectKey,
    DateTime UploadedAt);

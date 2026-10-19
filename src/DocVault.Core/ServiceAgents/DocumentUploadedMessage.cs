namespace DocVault.Core.ServiceAgents;

// Nachricht an die OCR-Queue, wird vom (spaeteren) OCR-Worker konsumiert.
public record DocumentUploadedMessage(
    Guid DocumentId,
    string FileName,
    string ContentType,
    DateTime UploadedAt);

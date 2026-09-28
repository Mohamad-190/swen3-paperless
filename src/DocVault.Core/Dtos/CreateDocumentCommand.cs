namespace DocVault.Core.Dtos;

public record CreateDocumentCommand(
    string Title,
    string FileName,
    string ContentType,
    long FileSize,
    Guid? FolderId);

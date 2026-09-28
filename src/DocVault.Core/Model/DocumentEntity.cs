namespace DocVault.Core.Model;

public class DocumentEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string? Summary { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public DateTime UploadedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? FolderId { get; set; }
}

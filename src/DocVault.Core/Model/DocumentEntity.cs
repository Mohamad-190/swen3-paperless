namespace DocVault.Core.Model;

public class DocumentEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    // Dateiname in RustFS/S3 (GUID, nicht der Originalname). Null, solange der Upload
    // in RustFS fehlschlaegt und das Dokument deshalb gar nicht erst gespeichert wird -
    // aktuell also praktisch nie, aber nullable fuer alte Daten vor dieser Aenderung.
    public string? ObjectKey { get; set; }

    public string? Summary { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public DateTime UploadedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? FolderId { get; set; }

    public FolderEntity? Folder { get; set; }

    public ICollection<TagEntity> Tags { get; set; } = new List<TagEntity>();
}

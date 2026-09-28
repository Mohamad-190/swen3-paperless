namespace DocVault.Core.Model;

public class FolderEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
}

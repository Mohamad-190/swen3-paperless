namespace DocVault.Core.Model;

public class TagEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
}

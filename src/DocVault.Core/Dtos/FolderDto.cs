namespace DocVault.Core.Dtos;

public class FolderDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public int DocumentCount { get; set; }
}

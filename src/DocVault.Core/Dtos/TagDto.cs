namespace DocVault.Core.Dtos;

public class TagDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public int DocumentCount { get; set; }
}

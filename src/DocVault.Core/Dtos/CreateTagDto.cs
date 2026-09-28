using System.ComponentModel.DataAnnotations;

namespace DocVault.Core.Dtos;

public class CreateTagDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Color { get; set; }
}

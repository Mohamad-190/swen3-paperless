using System.ComponentModel.DataAnnotations;

namespace DocVault.Core.Dtos;

public class UpdateFolderDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

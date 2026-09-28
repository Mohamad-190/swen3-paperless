using System.ComponentModel.DataAnnotations;

namespace DocVault.Core.Dtos;

public class UpdateDocumentDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    public Guid? FolderId { get; set; }
}

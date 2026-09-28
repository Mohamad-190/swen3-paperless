using System.ComponentModel.DataAnnotations;

namespace DocVault.WebApi.Models;

public class UploadDocumentRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    public Guid? FolderId { get; set; }
}

using DocVault.Core.Dtos;
using DocVault.Core.Model;

namespace DocVault.Core.Services;

public static class DocumentMapper
{
    public static DocumentDto ToDto(this DocumentEntity entity)
    {
        return new DocumentDto
        {
            Id = entity.Id,
            Title = entity.Title,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            Summary = entity.Summary,
            Status = entity.Status,
            UploadedAt = entity.UploadedAt,
            UpdatedAt = entity.UpdatedAt,
            FolderId = entity.FolderId
        };
    }
}

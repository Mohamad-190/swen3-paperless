using DocVault.Core.Dtos;
using DocVault.Core.Model;

namespace DocVault.Core.Services;

public static class FolderMapper
{
    public static FolderDto ToDto(this FolderEntity entity)
    {
        return new FolderDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            DocumentCount = entity.Documents.Count
        };
    }
}

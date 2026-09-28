using DocVault.Core.Dtos;

namespace DocVault.Core.Services;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct = default);

    Task<TagDto> CreateAsync(CreateTagDto dto, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task AssignToDocumentAsync(Guid documentId, Guid tagId, CancellationToken ct = default);

    Task RemoveFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken ct = default);
}

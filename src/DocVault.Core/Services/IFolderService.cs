using DocVault.Core.Dtos;

namespace DocVault.Core.Services;

public interface IFolderService
{
    Task<IReadOnlyList<FolderDto>> GetAllAsync(CancellationToken ct = default);

    Task<FolderDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<FolderDto> CreateAsync(CreateFolderDto dto, CancellationToken ct = default);

    Task<FolderDto> UpdateAsync(Guid id, UpdateFolderDto dto, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid id, CancellationToken ct = default);

    Task MoveDocumentAsync(Guid folderId, Guid documentId, CancellationToken ct = default);
}

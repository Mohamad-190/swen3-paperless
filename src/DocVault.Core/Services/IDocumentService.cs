using DocVault.Core.Dtos;

namespace DocVault.Core.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken ct = default);

    Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DocumentDto> CreateAsync(
        string title,
        string fileName,
        string contentType,
        long fileSize,
        Guid? folderId,
        CancellationToken ct = default);

    Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

using DocVault.Core.Model;

namespace DocVault.Core.Infrastructure.Repositories;

public interface IDocumentRepository
{
    Task<IReadOnlyList<DocumentEntity>> GetAllAsync(CancellationToken ct = default);

    Task<DocumentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DocumentEntity> AddAsync(DocumentEntity document, CancellationToken ct = default);

    Task UpdateAsync(DocumentEntity document, CancellationToken ct = default);

    Task DeleteAsync(DocumentEntity document, CancellationToken ct = default);
}

using DocVault.Core.Model;

namespace DocVault.Core.Infrastructure.Repositories;

public interface IFolderRepository
{
    Task<IReadOnlyList<FolderEntity>> GetAllAsync(CancellationToken ct = default);

    Task<FolderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<FolderEntity> AddAsync(FolderEntity folder, CancellationToken ct = default);

    Task UpdateAsync(FolderEntity folder, CancellationToken ct = default);

    Task DeleteAsync(FolderEntity folder, CancellationToken ct = default);
}

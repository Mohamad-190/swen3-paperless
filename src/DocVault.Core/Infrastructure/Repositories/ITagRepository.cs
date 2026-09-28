using DocVault.Core.Model;

namespace DocVault.Core.Infrastructure.Repositories;

public interface ITagRepository
{
    Task<IReadOnlyList<TagEntity>> GetAllAsync(CancellationToken ct = default);

    Task<TagEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<TagEntity> AddAsync(TagEntity tag, CancellationToken ct = default);

    Task DeleteAsync(TagEntity tag, CancellationToken ct = default);
}

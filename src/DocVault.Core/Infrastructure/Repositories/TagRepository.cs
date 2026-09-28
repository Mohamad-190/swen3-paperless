using DocVault.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DocVault.Core.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly DocVaultDbContext _context;

    public TagRepository(DocVaultDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TagEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Tags
            .AsNoTracking()
            .Include(t => t.Documents)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<TagEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tags
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<TagEntity> AddAsync(TagEntity tag, CancellationToken ct = default)
    {
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(ct);
        return tag;
    }

    public async Task DeleteAsync(TagEntity tag, CancellationToken ct = default)
    {
        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync(ct);
    }
}

using DocVault.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DocVault.Core.Infrastructure.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly DocVaultDbContext _context;

    public FolderRepository(DocVaultDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FolderEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Folders
            .AsNoTracking()
            .Include(f => f.Documents)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
    }

    public async Task<FolderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Folders
            .Include(f => f.Documents)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<FolderEntity> AddAsync(FolderEntity folder, CancellationToken ct = default)
    {
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync(ct);
        return folder;
    }

    public async Task UpdateAsync(FolderEntity folder, CancellationToken ct = default)
    {
        _context.Folders.Update(folder);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(FolderEntity folder, CancellationToken ct = default)
    {
        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync(ct);
    }
}

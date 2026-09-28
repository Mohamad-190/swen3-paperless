using DocVault.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DocVault.Core.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DocVaultDbContext _context;

    public DocumentRepository(DocVaultDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DocumentEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task<DocumentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<DocumentEntity> AddAsync(DocumentEntity document, CancellationToken ct = default)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(ct);
        return document;
    }

    public async Task UpdateAsync(DocumentEntity document, CancellationToken ct = default)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(DocumentEntity document, CancellationToken ct = default)
    {
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(ct);
    }
}

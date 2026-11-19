using DocVault.Core.Dtos;

namespace DocVault.Core.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken ct = default);

    Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Per OCR erkannter Text des Dokuments (Content ist null, solange er noch nicht vorliegt).
    Task<DocumentContentDto> GetContentAsync(Guid id, CancellationToken ct = default);

    Task<DocumentDto> CreateAsync(CreateDocumentCommand command, CancellationToken ct = default);

    // Volltext-/Fuzzy-Suche im per OCR erkannten Text, Titel und Dateinamen.
    Task<IReadOnlyList<DocumentDto>> SearchAsync(string query, CancellationToken ct = default);

    Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

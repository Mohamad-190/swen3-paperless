using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;

namespace DocVault.Core.Services;

public class DocumentService : IDocumentService
{
    private const int MaxTitleLength = 200;

    private readonly IDocumentRepository _repository;

    public DocumentService(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var documents = await _repository.GetAllAsync(ct);
        return documents.Select(d => d.ToDto()).ToList();
    }

    public async Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var document = await GetExistingAsync(id, ct);
        return document.ToDto();
    }

    public async Task<DocumentDto> CreateAsync(
        string title,
        string fileName,
        string contentType,
        long fileSize,
        Guid? folderId,
        CancellationToken ct = default)
    {
        ValidateTitle(title);

        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidDocumentException("File name must not be empty.");

        if (fileSize <= 0)
            throw new InvalidDocumentException("File must not be empty.");

        var document = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow,
            FolderId = folderId
        };

        var created = await _repository.AddAsync(document, ct);
        return created.ToDto();
    }

    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto, CancellationToken ct = default)
    {
        ValidateTitle(dto.Title);

        var document = await GetExistingAsync(id, ct);

        document.Title = dto.Title.Trim();
        document.FolderId = dto.FolderId;
        document.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(document, ct);
        return document.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var document = await GetExistingAsync(id, ct);
        await _repository.DeleteAsync(document, ct);
    }

    private async Task<DocumentEntity> GetExistingAsync(Guid id, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(id, ct)
            ?? throw new DocumentNotFoundException(id);
    }

    private static void ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidDocumentException("Title must not be empty.");

        if (title.Trim().Length > MaxTitleLength)
            throw new InvalidDocumentException($"Title must not exceed {MaxTitleLength} characters.");
    }
}

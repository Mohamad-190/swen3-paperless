using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;

namespace DocVault.Core.Services;

public class FolderService : IFolderService
{
    private const int MaxNameLength = 100;

    private readonly IFolderRepository _repository;
    private readonly IDocumentRepository _documentRepository;

    public FolderService(IFolderRepository repository, IDocumentRepository documentRepository)
    {
        _repository = repository;
        _documentRepository = documentRepository;
    }

    public async Task<IReadOnlyList<FolderDto>> GetAllAsync(CancellationToken ct = default)
    {
        var folders = await _repository.GetAllAsync(ct);
        return folders.Select(f => f.ToDto()).ToList();
    }

    public async Task<FolderDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await GetExistingAsync(id, ct);
        return folder.ToDto();
    }

    public async Task<FolderDto> CreateAsync(CreateFolderDto dto, CancellationToken ct = default)
    {
        ValidateName(dto.Name);

        var folder = new FolderEntity
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(folder, ct);
        return created.ToDto();
    }

    public async Task<FolderDto> UpdateAsync(Guid id, UpdateFolderDto dto, CancellationToken ct = default)
    {
        ValidateName(dto.Name);

        var folder = await GetExistingAsync(id, ct);

        folder.Name = dto.Name.Trim();
        folder.Description = dto.Description;

        await _repository.UpdateAsync(folder, ct);
        return folder.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await GetExistingAsync(id, ct);

        if (folder.Documents.Count > 0)
            throw new InvalidFolderException("Folder must be empty before it can be deleted.");

        await _repository.DeleteAsync(folder, ct);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await GetExistingAsync(id, ct);
        return folder.Documents.Select(d => d.ToDto()).ToList();
    }

    public async Task MoveDocumentAsync(Guid folderId, Guid documentId, CancellationToken ct = default)
    {
        await GetExistingAsync(folderId, ct);

        var document = await _documentRepository.GetByIdAsync(documentId, ct)
            ?? throw new DocumentNotFoundException(documentId);

        document.FolderId = folderId;

        await _documentRepository.UpdateAsync(document, ct);
    }

    private async Task<FolderEntity> GetExistingAsync(Guid id, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(id, ct)
            ?? throw new FolderNotFoundException(id);
    }

    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidFolderException("Name must not be empty.");

        if (name.Trim().Length > MaxNameLength)
            throw new InvalidFolderException($"Name must not exceed {MaxNameLength} characters.");
    }
}

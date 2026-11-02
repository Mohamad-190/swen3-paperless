using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using Microsoft.Extensions.Logging;

namespace DocVault.Core.Services;

public class FolderService : IFolderService
{
    private const int MaxNameLength = 100;

    private readonly IFolderRepository _repository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<FolderService> _logger;

    public FolderService(IFolderRepository repository, IDocumentRepository documentRepository, ILogger<FolderService> logger)
    {
        _repository = repository;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FolderDto>> GetAllAsync(CancellationToken ct = default)
    {
        var folders = await _repository.GetAllAsync(ct);
        return folders.Select(ToDto).ToList();
    }

    public async Task<FolderDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await GetExistingAsync(id, ct);
        return ToDto(folder);
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
        _logger.LogInformation("Folder {FolderId} created ({Name})", created.Id, created.Name);
        return ToDto(created);
    }

    public async Task<FolderDto> UpdateAsync(Guid id, UpdateFolderDto dto, CancellationToken ct = default)
    {
        ValidateName(dto.Name);

        var folder = await GetExistingAsync(id, ct);

        folder.Name = dto.Name.Trim();
        folder.Description = dto.Description;

        await _repository.UpdateAsync(folder, ct);
        return ToDto(folder);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await GetExistingAsync(id, ct);

        if (folder.Documents.Count > 0)
        {
            _logger.LogWarning("Deletion of folder {FolderId} blocked: still contains {DocumentCount} document(s)",
                id, folder.Documents.Count);
            throw new InvalidFolderException("Folder must be empty before it can be deleted.");
        }

        await _repository.DeleteAsync(folder, ct);
        _logger.LogInformation("Folder {FolderId} deleted", id);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await GetExistingAsync(id, ct);
        return folder.Documents.Select(DocumentToDto).ToList();
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
        var folder = await _repository.GetByIdAsync(id, ct);
        if (folder is null)
        {
            _logger.LogWarning("Folder {FolderId} was requested but does not exist", id);
            throw new FolderNotFoundException(id);
        }
        return folder;
    }

    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidFolderException("Name must not be empty.");

        if (name.Trim().Length > MaxNameLength)
            throw new InvalidFolderException($"Name must not exceed {MaxNameLength} characters.");
    }

    private static FolderDto ToDto(FolderEntity entity)
    {
        return new FolderDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            DocumentCount = entity.Documents.Count
        };
    }

    private static DocumentDto DocumentToDto(DocumentEntity entity)
    {
        return new DocumentDto
        {
            Id = entity.Id,
            Title = entity.Title,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            Summary = entity.Summary,
            Status = entity.Status,
            UploadedAt = entity.UploadedAt,
            UpdatedAt = entity.UpdatedAt,
            FolderId = entity.FolderId
        };
    }
}

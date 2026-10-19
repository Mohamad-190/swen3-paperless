using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using DocVault.Core.ServiceAgents;

namespace DocVault.Core.Services;

public class DocumentService : IDocumentService
{
    private const int MaxTitleLength = 200;
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg"
    };

    private readonly IDocumentRepository _repository;
    private readonly IDocumentQueuePublisher _queuePublisher;

    public DocumentService(IDocumentRepository repository, IDocumentQueuePublisher queuePublisher)
    {
        _repository = repository;
        _queuePublisher = queuePublisher;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var documents = await _repository.GetAllAsync(ct);
        return documents.Select(ToDto).ToList();
    }

    public async Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var document = await GetExistingAsync(id, ct);
        return ToDto(document);
    }

    public async Task<DocumentDto> CreateAsync(CreateDocumentCommand command, CancellationToken ct = default)
    {
        ValidateTitle(command.Title);

        if (string.IsNullOrWhiteSpace(command.FileName))
            throw new InvalidDocumentException("File name must not be empty.");

        if (command.FileSize <= 0)
            throw new InvalidDocumentException("File must not be empty.");

        if (command.FileSize > MaxFileSizeBytes)
            throw new InvalidDocumentException($"File must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (!AllowedContentTypes.Contains(command.ContentType))
            throw new InvalidDocumentException(
                $"File type '{command.ContentType}' is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}.");

        var document = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Title = command.Title.Trim(),
            FileName = command.FileName,
            ContentType = command.ContentType,
            FileSize = command.FileSize,
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow,
            FolderId = command.FolderId
        };

        var created = await _repository.AddAsync(document, ct);

        // Erst nach dem Speichern publishen, damit der Worker das Dokument in der DB findet.
        await _queuePublisher.PublishDocumentUploadedAsync(
            new DocumentUploadedMessage(created.Id, created.FileName, created.ContentType, created.UploadedAt), ct);

        return ToDto(created);
    }

    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto, CancellationToken ct = default)
    {
        ValidateTitle(dto.Title);

        var document = await GetExistingAsync(id, ct);

        document.Title = dto.Title.Trim();
        document.FolderId = dto.FolderId;
        document.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(document, ct);
        return ToDto(document);
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

    private static DocumentDto ToDto(DocumentEntity entity)
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

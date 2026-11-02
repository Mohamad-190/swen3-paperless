using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using Microsoft.Extensions.Logging;

namespace DocVault.Core.Services;

public class TagService : ITagService
{
    private const int MaxNameLength = 50;

    private readonly ITagRepository _repository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<TagService> _logger;

    public TagService(ITagRepository repository, IDocumentRepository documentRepository, ILogger<TagService> logger)
    {
        _repository = repository;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tags = await _repository.GetAllAsync(ct);
        return tags.Select(ToDto).ToList();
    }

    public async Task<TagDto> CreateAsync(CreateTagDto dto, CancellationToken ct = default)
    {
        ValidateName(dto.Name);

        var tag = new TagEntity
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Color = dto.Color
        };

        var created = await _repository.AddAsync(tag, ct);
        _logger.LogInformation("Tag {TagId} created ({Name})", created.Id, created.Name);
        return ToDto(created);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await _repository.GetByIdAsync(id, ct);
        if (tag is null)
        {
            _logger.LogWarning("Tag {TagId} was requested but does not exist", id);
            throw new TagNotFoundException(id);
        }

        await _repository.DeleteAsync(tag, ct);
        _logger.LogInformation("Tag {TagId} deleted", id);
    }

    public async Task AssignToDocumentAsync(Guid documentId, Guid tagId, CancellationToken ct = default)
    {
        var document = await _documentRepository.GetByIdWithTagsAsync(documentId, ct)
            ?? throw new DocumentNotFoundException(documentId);

        var tag = await _repository.GetByIdAsync(tagId, ct)
            ?? throw new TagNotFoundException(tagId);

        if (document.Tags.Any(t => t.Id == tagId))
            return;

        document.Tags.Add(tag);
        await _documentRepository.UpdateAsync(document, ct);
        _logger.LogInformation("Tag {TagId} assigned to document {DocumentId}", tagId, documentId);
    }

    public async Task RemoveFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken ct = default)
    {
        var document = await _documentRepository.GetByIdWithTagsAsync(documentId, ct)
            ?? throw new DocumentNotFoundException(documentId);

        var tag = document.Tags.FirstOrDefault(t => t.Id == tagId);
        if (tag is null)
            return;

        document.Tags.Remove(tag);
        await _documentRepository.UpdateAsync(document, ct);
    }

    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidTagException("Name must not be empty.");

        if (name.Trim().Length > MaxNameLength)
            throw new InvalidTagException($"Name must not exceed {MaxNameLength} characters.");
    }

    private static TagDto ToDto(TagEntity entity)
    {
        return new TagDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Color = entity.Color,
            DocumentCount = entity.Documents.Count
        };
    }
}

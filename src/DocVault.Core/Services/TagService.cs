using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;

namespace DocVault.Core.Services;

public class TagService : ITagService
{
    private const int MaxNameLength = 50;

    private readonly ITagRepository _repository;
    private readonly IDocumentRepository _documentRepository;

    public TagService(ITagRepository repository, IDocumentRepository documentRepository)
    {
        _repository = repository;
        _documentRepository = documentRepository;
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
        return ToDto(created);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await _repository.GetByIdAsync(id, ct)
            ?? throw new TagNotFoundException(id);

        await _repository.DeleteAsync(tag, ct);
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

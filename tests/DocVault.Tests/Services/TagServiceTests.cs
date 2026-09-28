using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using DocVault.Core.Services;
using NSubstitute;

namespace DocVault.Tests.Services;

public class TagServiceTests
{
    private readonly ITagRepository _repository = Substitute.For<ITagRepository>();
    private readonly IDocumentRepository _documentRepository = Substitute.For<IDocumentRepository>();
    private readonly TagService _service;

    public TagServiceTests()
    {
        _service = new TagService(_repository, _documentRepository);

        _repository.AddAsync(Arg.Any<TagEntity>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<TagEntity>());
    }

    [Fact]
    public async Task CreateAsync_ValidInput_SavesTag()
    {
        var result = await _service.CreateAsync(new CreateTagDto { Name = "  Invoice  ", Color = "#FF0000" });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Invoice", result.Name);
        Assert.Equal("#FF0000", result.Color);
        await _repository.Received(1).AddAsync(
            Arg.Is<TagEntity>(t => t.Name == "Invoice"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsAndDoesNotSave()
    {
        await Assert.ThrowsAsync<InvalidTagException>(
            () => _service.CreateAsync(new CreateTagDto { Name = "   " }));

        await _repository.DidNotReceive().AddAsync(Arg.Any<TagEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_UnknownTag_ThrowsNotFound()
    {
        var tagId = Guid.NewGuid();
        _repository.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns((TagEntity?)null);

        var ex = await Assert.ThrowsAsync<TagNotFoundException>(() => _service.DeleteAsync(tagId));

        Assert.Equal(tagId, ex.TagId);
    }

    [Fact]
    public async Task AssignToDocumentAsync_ExistingDocumentAndTag_AddsTag()
    {
        var document = new DocumentEntity { Id = Guid.NewGuid(), Title = "Invoice" };
        var tag = new TagEntity { Id = Guid.NewGuid(), Name = "Invoice" };
        _documentRepository.GetByIdWithTagsAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);
        _repository.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(tag);

        await _service.AssignToDocumentAsync(document.Id, tag.Id);

        Assert.Contains(tag, document.Tags);
        await _documentRepository.Received(1).UpdateAsync(document, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignToDocumentAsync_UnknownDocument_ThrowsNotFound()
    {
        var documentId = Guid.NewGuid();
        _documentRepository.GetByIdWithTagsAsync(documentId, Arg.Any<CancellationToken>())
            .Returns((DocumentEntity?)null);

        await Assert.ThrowsAsync<DocumentNotFoundException>(
            () => _service.AssignToDocumentAsync(documentId, Guid.NewGuid()));
    }

    [Fact]
    public async Task AssignToDocumentAsync_UnknownTag_ThrowsNotFound()
    {
        var document = new DocumentEntity { Id = Guid.NewGuid(), Title = "Invoice" };
        _documentRepository.GetByIdWithTagsAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TagEntity?)null);

        await Assert.ThrowsAsync<TagNotFoundException>(
            () => _service.AssignToDocumentAsync(document.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveFromDocumentAsync_ExistingTag_RemovesTag()
    {
        var document = new DocumentEntity { Id = Guid.NewGuid(), Title = "Invoice" };
        var tag = new TagEntity { Id = Guid.NewGuid(), Name = "Invoice" };
        document.Tags.Add(tag);
        _documentRepository.GetByIdWithTagsAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        await _service.RemoveFromDocumentAsync(document.Id, tag.Id);

        Assert.DoesNotContain(tag, document.Tags);
        await _documentRepository.Received(1).UpdateAsync(document, Arg.Any<CancellationToken>());
    }
}

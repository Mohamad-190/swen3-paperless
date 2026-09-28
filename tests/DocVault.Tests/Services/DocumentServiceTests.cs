using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using DocVault.Core.Services;
using NSubstitute;

namespace DocVault.Tests.Services;

public class DocumentServiceTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _service = new DocumentService(_repository);

        _repository.AddAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<DocumentEntity>());
    }

    private static DocumentEntity CreateEntity(string title = "Invoice") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        FileName = "invoice.pdf",
        ContentType = "application/pdf",
        FileSize = 1024,
        Status = DocumentStatus.Uploaded,
        UploadedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAsync_ValidInput_SavesDocumentWithUploadedStatus()
    {
        var result = await _service.CreateAsync("  Invoice  ", "invoice.pdf", "application/pdf", 1024, null);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Invoice", result.Title);
        Assert.Equal(DocumentStatus.Uploaded, result.Status);
        await _repository.Received(1).AddAsync(
            Arg.Is<DocumentEntity>(d => d.FileName == "invoice.pdf" && d.FileSize == 1024),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ThrowsAndDoesNotSave()
    {
        await Assert.ThrowsAsync<InvalidDocumentException>(
            () => _service.CreateAsync("   ", "invoice.pdf", "application/pdf", 1024, null));

        await _repository.DidNotReceive().AddAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((DocumentEntity?)null);

        var ex = await Assert.ThrowsAsync<DocumentNotFoundException>(() => _service.GetByIdAsync(id));

        Assert.Equal(id, ex.DocumentId);
    }

    [Fact]
    public async Task UpdateAsync_ExistingDocument_UpdatesTitleAndTimestamp()
    {
        var entity = CreateEntity("Old");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _service.UpdateAsync(entity.Id, new UpdateDocumentDto { Title = "New" });

        Assert.Equal("New", result.Title);
        Assert.NotNull(result.UpdatedAt);
        await _repository.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((DocumentEntity?)null);

        await Assert.ThrowsAsync<DocumentNotFoundException>(
            () => _service.UpdateAsync(id, new UpdateDocumentDto { Title = "New" }));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ExistingDocument_DeletesIt()
    {
        var entity = CreateEntity();
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);

        await _service.DeleteAsync(entity.Id);

        await _repository.Received(1).DeleteAsync(entity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((DocumentEntity?)null);

        await Assert.ThrowsAsync<DocumentNotFoundException>(() => _service.DeleteAsync(id));

        await _repository.DidNotReceive().DeleteAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>());
    }
}

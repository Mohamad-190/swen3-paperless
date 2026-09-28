using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using DocVault.Core.Services;
using NSubstitute;

namespace DocVault.Tests.Services;

public class FolderServiceTests
{
    private readonly IFolderRepository _repository = Substitute.For<IFolderRepository>();
    private readonly IDocumentRepository _documentRepository = Substitute.For<IDocumentRepository>();
    private readonly FolderService _service;

    public FolderServiceTests()
    {
        _service = new FolderService(_repository, _documentRepository);

        _repository.AddAsync(Arg.Any<FolderEntity>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<FolderEntity>());
    }

    [Fact]
    public async Task CreateAsync_ValidInput_SavesFolder()
    {
        var result = await _service.CreateAsync(new CreateFolderDto { Name = "  Invoices  ", Description = "Scans" });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Invoices", result.Name);
        Assert.Equal("Scans", result.Description);
        await _repository.Received(1).AddAsync(
            Arg.Is<FolderEntity>(f => f.Name == "Invoices"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsAndDoesNotSave()
    {
        await Assert.ThrowsAsync<InvalidFolderException>(
            () => _service.CreateAsync(new CreateFolderDto { Name = "   " }));

        await _repository.DidNotReceive().AddAsync(Arg.Any<FolderEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_FolderWithDocuments_ThrowsAndDoesNotDelete()
    {
        var folder = new FolderEntity { Id = Guid.NewGuid(), Name = "Invoices", CreatedAt = DateTime.UtcNow };
        folder.Documents.Add(new DocumentEntity { Id = Guid.NewGuid(), Title = "Invoice" });
        _repository.GetByIdAsync(folder.Id, Arg.Any<CancellationToken>()).Returns(folder);

        await Assert.ThrowsAsync<InvalidFolderException>(() => _service.DeleteAsync(folder.Id));

        await _repository.DidNotReceive().DeleteAsync(Arg.Any<FolderEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_EmptyFolder_DeletesIt()
    {
        var folder = new FolderEntity { Id = Guid.NewGuid(), Name = "Empty", CreatedAt = DateTime.UtcNow };
        _repository.GetByIdAsync(folder.Id, Arg.Any<CancellationToken>()).Returns(folder);

        await _service.DeleteAsync(folder.Id);

        await _repository.Received(1).DeleteAsync(folder, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveDocumentAsync_UnknownFolder_ThrowsNotFound()
    {
        var folderId = Guid.NewGuid();
        _repository.GetByIdAsync(folderId, Arg.Any<CancellationToken>()).Returns((FolderEntity?)null);

        var ex = await Assert.ThrowsAsync<FolderNotFoundException>(
            () => _service.MoveDocumentAsync(folderId, Guid.NewGuid()));

        Assert.Equal(folderId, ex.FolderId);
        await _documentRepository.DidNotReceive().UpdateAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveDocumentAsync_ExistingFolderAndDocument_SetsFolderId()
    {
        var folder = new FolderEntity { Id = Guid.NewGuid(), Name = "Invoices", CreatedAt = DateTime.UtcNow };
        var document = new DocumentEntity { Id = Guid.NewGuid(), Title = "Invoice" };
        _repository.GetByIdAsync(folder.Id, Arg.Any<CancellationToken>()).Returns(folder);
        _documentRepository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        await _service.MoveDocumentAsync(folder.Id, document.Id);

        Assert.Equal(folder.Id, document.FolderId);
        await _documentRepository.Received(1).UpdateAsync(document, Arg.Any<CancellationToken>());
    }
}

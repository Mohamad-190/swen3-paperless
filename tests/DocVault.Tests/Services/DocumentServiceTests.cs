using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Model;
using DocVault.Core.ServiceAgents;
using DocVault.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DocVault.Tests.Services;

public class DocumentServiceTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IDocumentQueuePublisher _queuePublisher = Substitute.For<IDocumentQueuePublisher>();
    private readonly IDocumentStorage _storage = Substitute.For<IDocumentStorage>();
    private readonly ISearchIndexer _searchIndexer = Substitute.For<ISearchIndexer>();
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _service = new DocumentService(_repository, _queuePublisher, _storage, _searchIndexer, NullLogger<DocumentService>.Instance);

        _repository.AddAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<DocumentEntity>());
    }

    // Helper, damit die Tests nicht ueberall "new MemoryStream()" wiederholen muessen.
    private static CreateDocumentCommand CreateCommand(string title, string fileName, string contentType, long fileSize, Guid? folderId = null) =>
        new(title, fileName, contentType, fileSize, folderId, new MemoryStream());

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
        var result = await _service.CreateAsync(CreateCommand("  Invoice  ", "invoice.pdf", "application/pdf", 1024, null));

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
            () => _service.CreateAsync(CreateCommand("   ", "invoice.pdf", "application/pdf", 1024, null)));

        await _repository.DidNotReceive().AddAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ValidInput_PublishesUploadedMessage()
    {
        string? uploadedKey = null;
        _storage
            .When(s => s.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(call => uploadedKey = call.ArgAt<string>(0));

        var result = await _service.CreateAsync(CreateCommand("Invoice", "invoice.pdf", "application/pdf", 1024, null));

        // Der Worker muss die Datei unter genau dem Key finden, unter dem sie hochgeladen wurde.
        await _queuePublisher.Received(1).PublishDocumentUploadedAsync(
            Arg.Is<DocumentUploadedMessage>(m =>
                m.DocumentId == result.Id &&
                m.Title == "Invoice" &&
                m.FileName == "invoice.pdf" &&
                m.ContentType == "application/pdf" &&
                m.ObjectKey == uploadedKey),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_InvalidInput_DoesNotPublish()
    {
        await Assert.ThrowsAsync<InvalidDocumentException>(
            () => _service.CreateAsync(CreateCommand("   ", "invoice.pdf", "application/pdf", 1024, null)));

        await _queuePublisher.DidNotReceive().PublishDocumentUploadedAsync(
            Arg.Any<DocumentUploadedMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_StorageFails_DoesNotSaveOrPublish()
    {
        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new StorageException("storage down", new IOException()));

        await Assert.ThrowsAsync<StorageException>(
            () => _service.CreateAsync(CreateCommand("Invoice", "invoice.pdf", "application/pdf", 1024, null)));

        await _repository.DidNotReceive().AddAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>());
        await _queuePublisher.DidNotReceive().PublishDocumentUploadedAsync(
            Arg.Any<DocumentUploadedMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SaveFails_DoesNotPublish()
    {
        _repository.AddAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>())
            .Returns<DocumentEntity>(_ => throw new InvalidOperationException("db down"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(CreateCommand("Invoice", "invoice.pdf", "application/pdf", 1024, null)));

        await _queuePublisher.DidNotReceive().PublishDocumentUploadedAsync(
            Arg.Any<DocumentUploadedMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PublishFails_MarksDocumentAsFailed()
    {
        _queuePublisher.PublishDocumentUploadedAsync(Arg.Any<DocumentUploadedMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new MessagingException("broker down", new IOException()));

        await Assert.ThrowsAsync<MessagingException>(
            () => _service.CreateAsync(CreateCommand("Invoice", "invoice.pdf", "application/pdf", 1024, null)));

        await _repository.Received(1).UpdateAsync(
            Arg.Is<DocumentEntity>(d => d.Status == DocumentStatus.Failed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DisallowedContentType_ThrowsAndDoesNotSave()
    {
        await Assert.ThrowsAsync<InvalidDocumentException>(
            () => _service.CreateAsync(CreateCommand("Invoice", "invoice.exe", "application/x-msdownload", 1024, null)));

        await _repository.DidNotReceive().AddAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_FileTooLarge_ThrowsAndDoesNotSave()
    {
        const long tooLarge = 21L * 1024 * 1024;

        await Assert.ThrowsAsync<InvalidDocumentException>(
            () => _service.CreateAsync(CreateCommand("Invoice", "invoice.pdf", "application/pdf", tooLarge, null)));

        await _repository.DidNotReceive().AddAsync(Arg.Any<DocumentEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_Throws()
    {
        await Assert.ThrowsAsync<InvalidDocumentException>(() => _service.SearchAsync("   "));

        await _searchIndexer.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ReturnsMatchingDocumentsInOrder()
    {
        var first = CreateEntity("First");
        var second = CreateEntity("Second");
        _searchIndexer.SearchAsync("hello", Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { first.Id, second.Id });
        _repository.GetByIdAsync(first.Id, Arg.Any<CancellationToken>()).Returns(first);
        _repository.GetByIdAsync(second.Id, Arg.Any<CancellationToken>()).Returns(second);

        var results = await _service.SearchAsync("hello");

        Assert.Equal(new[] { first.Id, second.Id }, results.Select(r => r.Id));
    }

    [Fact]
    public async Task SearchAsync_DeletedDocumentStillInIndex_IsSkipped()
    {
        var deletedId = Guid.NewGuid();
        var existing = CreateEntity("Existing");
        _searchIndexer.SearchAsync("hello", Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { deletedId, existing.Id });
        _repository.GetByIdAsync(deletedId, Arg.Any<CancellationToken>()).Returns((DocumentEntity?)null);
        _repository.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var results = await _service.SearchAsync("hello");

        Assert.Single(results);
        Assert.Equal(existing.Id, results[0].Id);
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

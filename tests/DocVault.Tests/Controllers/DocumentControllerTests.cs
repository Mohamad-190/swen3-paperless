using DocVault.Core.Dtos;
using DocVault.Core.Services;
using DocVault.WebApi.Controllers;
using DocVault.WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DocVault.Tests.Controllers;

public class DocumentControllerTests
{
    private readonly IDocumentService _service = Substitute.For<IDocumentService>();
    private readonly DocumentController _controller;

    public DocumentControllerTests()
    {
        _controller = new DocumentController(_service);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithDocuments()
    {
        var documents = new List<DocumentDto> { new() { Id = Guid.NewGuid(), Title = "Invoice" } };
        _service.GetAllAsync(Arg.Any<CancellationToken>()).Returns(documents);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(documents, ok.Value);
    }

    [Fact]
    public async Task Upload_PassesFileMetadataAndReturnsCreated()
    {
        var created = new DocumentDto { Id = Guid.NewGuid(), Title = "Invoice" };
        _service.CreateAsync(Arg.Any<CreateDocumentCommand>(), Arg.Any<CancellationToken>()).Returns(created);

        var content = new MemoryStream(new byte[2048]);
        var request = new UploadDocumentRequest
        {
            Title = "Invoice",
            File = new FormFile(content, 0, content.Length, "File", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            }
        };

        var result = await _controller.Upload(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(DocumentController.GetById), createdResult.ActionName);
        Assert.Same(created, createdResult.Value);
        await _service.Received(1).CreateAsync(
            Arg.Is<CreateDocumentCommand>(c =>
                c.Title == "Invoice" &&
                c.FileName == "invoice.pdf" &&
                c.ContentType == "application/pdf" &&
                c.FileSize == 2048),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var id = Guid.NewGuid();

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        await _service.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}

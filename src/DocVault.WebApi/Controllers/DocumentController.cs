using DocVault.Core.Dtos;
using DocVault.Core.Services;
using DocVault.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.WebApi.Controllers;

// Fehler (404, 400, 503, ...) uebersetzt der GlobalExceptionHandler.
[ApiController]
[Route("documents")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetAll(CancellationToken ct)
    {
        var documents = await _documentService.GetAllAsync(ct);
        return Ok(documents);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await _documentService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadDocumentRequest request, CancellationToken ct)
    {
        var command = new CreateDocumentCommand(
            request.Title,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            request.FolderId);

        var created = await _documentService.CreateAsync(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> Update(Guid id, UpdateDocumentDto dto, CancellationToken ct)
    {
        return Ok(await _documentService.UpdateAsync(id, dto, ct));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _documentService.DeleteAsync(id, ct);
        return NoContent();
    }
}

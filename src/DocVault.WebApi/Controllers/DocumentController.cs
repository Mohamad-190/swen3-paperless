using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Services;
using DocVault.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.WebApi.Controllers;

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
        try
        {
            return Ok(await _documentService.GetByIdAsync(id, ct));
        }
        catch (DocumentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadDocumentRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _documentService.CreateAsync(
                request.Title,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                request.FolderId,
                ct);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidDocumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> Update(Guid id, UpdateDocumentDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _documentService.UpdateAsync(id, dto, ct));
        }
        catch (DocumentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidDocumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _documentService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (DocumentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

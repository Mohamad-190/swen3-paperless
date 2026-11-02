using DocVault.Core.Dtos;
using DocVault.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.WebApi.Controllers;

// Fehler (404, 400, ...) uebersetzt der GlobalExceptionHandler.
[ApiController]
[Route("folders")]
public class FolderController : ControllerBase
{
    private readonly IFolderService _folderService;

    public FolderController(IFolderService folderService)
    {
        _folderService = folderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FolderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FolderDto>>> GetAll(CancellationToken ct)
    {
        var folders = await _folderService.GetAllAsync(ct);
        return Ok(folders);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FolderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderDto>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await _folderService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(FolderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FolderDto>> Create(CreateFolderDto dto, CancellationToken ct)
    {
        var created = await _folderService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FolderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderDto>> Update(Guid id, UpdateFolderDto dto, CancellationToken ct)
    {
        return Ok(await _folderService.UpdateAsync(id, dto, ct));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _folderService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/documents")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetDocuments(Guid id, CancellationToken ct)
    {
        return Ok(await _folderService.GetDocumentsAsync(id, ct));
    }

    [HttpPut("{id:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        await _folderService.MoveDocumentAsync(id, documentId, ct);
        return NoContent();
    }
}

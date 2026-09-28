using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.WebApi.Controllers;

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
        try
        {
            return Ok(await _folderService.GetByIdAsync(id, ct));
        }
        catch (FolderNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(FolderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FolderDto>> Create(CreateFolderDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _folderService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidFolderException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FolderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderDto>> Update(Guid id, UpdateFolderDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _folderService.UpdateAsync(id, dto, ct));
        }
        catch (FolderNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidFolderException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _folderService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (FolderNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidFolderException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}/documents")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetDocuments(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _folderService.GetDocumentsAsync(id, ct));
        }
        catch (FolderNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{id:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        try
        {
            await _folderService.MoveDocumentAsync(id, documentId, ct);
            return NoContent();
        }
        catch (FolderNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (DocumentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

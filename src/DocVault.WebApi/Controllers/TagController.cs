using DocVault.Core.Dtos;
using DocVault.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.WebApi.Controllers;

// Fehler (404, 400, ...) uebersetzt der GlobalExceptionHandler.
[ApiController]
[Route("tags")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _tagService.GetAllAsync(ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TagDto>> Create(CreateTagDto dto, CancellationToken ct)
    {
        var created = await _tagService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _tagService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("/documents/{documentId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToDocument(Guid documentId, Guid tagId, CancellationToken ct)
    {
        await _tagService.AssignToDocumentAsync(documentId, tagId, ct);
        return NoContent();
    }

    [HttpDelete("/documents/{documentId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromDocument(Guid documentId, Guid tagId, CancellationToken ct)
    {
        await _tagService.RemoveFromDocumentAsync(documentId, tagId, ct);
        return NoContent();
    }
}

using DocVault.Core.Dtos;
using DocVault.Core.Exceptions;
using DocVault.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.WebApi.Controllers;

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
        try
        {
            var created = await _tagService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (InvalidTagException ex)
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
            await _tagService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (TagNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("/documents/{documentId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToDocument(Guid documentId, Guid tagId, CancellationToken ct)
    {
        try
        {
            await _tagService.AssignToDocumentAsync(documentId, tagId, ct);
            return NoContent();
        }
        catch (DocumentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (TagNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("/documents/{documentId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromDocument(Guid documentId, Guid tagId, CancellationToken ct)
    {
        try
        {
            await _tagService.RemoveFromDocumentAsync(documentId, tagId, ct);
            return NoContent();
        }
        catch (DocumentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

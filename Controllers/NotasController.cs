using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeScribe.Api.Auth;
using SafeScribe.Api.Data;
using SafeScribe.Api.Dtos;
using SafeScribe.Api.Models;
using System.IdentityModel.Tokens.Jwt;

namespace SafeScribe.Api.Controllers;

[ApiController]
[Route("api/v1/notas")]
[Authorize]
public class NotasController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotasController(AppDbContext db) => _db = db;

    private Guid GetUserId()
    {
        var sid = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(sid!);
    }

    private bool IsAdmin() => User.IsInRole(Roles.Admin);

    [HttpPost]
    [Authorize(Roles = Roles.Editor + "," + Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] NoteCreateDto dto)
    {
        var userId = GetUserId();
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Content = dto.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            UserId = userId
        };
        _db.Notes.Add(note);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = note.Id }, note);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var note = await _db.Notes.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id);
        if (note is null) return NotFound();

        if (!IsAdmin() && note.UserId != GetUserId())
            return Forbid();

        return Ok(note);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] NoteUpdateDto dto)
    {
        var note = await _db.Notes.SingleOrDefaultAsync(n => n.Id == id);
        if (note is null) return NotFound();

        if (!IsAdmin() && note.UserId != GetUserId())
            return Forbid();

        note.Title = dto.Title;
        note.Content = dto.Content;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var note = await _db.Notes.SingleOrDefaultAsync(n => n.Id == id);
        if (note is null) return NotFound();

        _db.Notes.Remove(note);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

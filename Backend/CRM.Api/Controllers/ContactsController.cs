using CRM.Api.Data;
using CRM.Api.Models;
using CRM.Api.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/contacts")]
public sealed class ContactsController : ControllerBase
{
    private readonly CrmDbContext _db;

    public ContactsController(CrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid workspaceId,
        [FromQuery] Guid? companyId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (workspaceId == Guid.Empty || page < 1 || pageSize is < 1 or > 200)
            return BadRequest(new { message = "Valid workspaceId, page and pageSize are required." });
        if (!await IsMember(workspaceId))
            return NotFound();

        var query = _db.Contacts.AsNoTracking().Where(x => x.WorkspaceId == workspaceId);
        if (companyId.HasValue)
            query = query.Where(x => x.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => EF.Functions.ILike(x.FullName, $"%{search.Trim()}%")
                || (x.Email != null && EF.Functions.ILike(x.Email, $"%{search.Trim()}%")));

        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.FullName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ContactResponse(x.Id, x.WorkspaceId, x.CompanyId, x.FullName,
                x.Email, x.Phone, x.JobTitle, x.Notes, x.CreatedAt, x.UpdatedAt))
            .ToListAsync();
        return Ok(new { items, page, pageSize, total });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            return BadRequest(new { message = "WorkspaceId is required." });
        if (!await IsMember(workspaceId))
            return NotFound();

        var item = await _db.Contacts.AsNoTracking()
            .Where(x => x.Id == id && x.WorkspaceId == workspaceId)
            .Select(x => new ContactResponse(x.Id, x.WorkspaceId, x.CompanyId, x.FullName,
                x.Email, x.Phone, x.JobTitle, x.Notes, x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContactRequest request)
    {
        if (request.WorkspaceId == Guid.Empty)
            return BadRequest(new { message = "WorkspaceId is required." });
        if (!await CanEdit(request.WorkspaceId))
            return NotFound();
        if (request.CompanyId.HasValue && !await _db.Companies.AnyAsync(
                x => x.Id == request.CompanyId && x.WorkspaceId == request.WorkspaceId))
            return BadRequest(new { message = "Company must belong to the specified workspace." });

        var now = DateTime.UtcNow;
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            CompanyId = request.CompanyId,
            FullName = request.FullName.Trim(),
            Email = request.Email,
            Phone = request.Phone,
            JobTitle = request.JobTitle,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = contact.Id, workspaceId = contact.WorkspaceId },
            new ContactResponse(contact.Id, contact.WorkspaceId, contact.CompanyId, contact.FullName,
                contact.Email, contact.Phone, contact.JobTitle, contact.Notes, contact.CreatedAt, contact.UpdatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromQuery] Guid workspaceId, ContactRequest request)
    {
        if (workspaceId == Guid.Empty || request.WorkspaceId != workspaceId)
            return BadRequest(new { message = "WorkspaceId must match the request body." });
        if (!await CanEdit(workspaceId))
            return NotFound();
        if (request.CompanyId.HasValue && !await _db.Companies.AnyAsync(
                x => x.Id == request.CompanyId && x.WorkspaceId == workspaceId))
            return BadRequest(new { message = "Company must belong to the specified workspace." });

        var contact = await _db.Contacts.SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == workspaceId);
        if (contact is null)
            return NotFound();
        contact.CompanyId = request.CompanyId;
        contact.FullName = request.FullName.Trim();
        contact.Email = request.Email;
        contact.Phone = request.Phone;
        contact.JobTitle = request.JobTitle;
        contact.Notes = request.Notes;
        contact.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new ContactResponse(contact.Id, contact.WorkspaceId, contact.CompanyId, contact.FullName,
            contact.Email, contact.Phone, contact.JobTitle, contact.Notes, contact.CreatedAt, contact.UpdatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty || !await CanEdit(workspaceId))
            return NotFound();
        var contact = await _db.Contacts.SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == workspaceId);
        if (contact is null)
            return NotFound();
        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private System.Threading.Tasks.Task<bool> IsMember(Guid workspaceId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return System.Threading.Tasks.Task.FromResult(false);
        return WorkspaceAccess.IsMemberAsync(_db, workspaceId, userId);
    }

    private System.Threading.Tasks.Task<bool> CanEdit(Guid workspaceId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return System.Threading.Tasks.Task.FromResult(false);
        return WorkspaceAccess.CanEditAsync(_db, workspaceId, userId);
    }
}

public sealed record ContactRequest(
    Guid WorkspaceId,
    string FullName,
    Guid? CompanyId,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Notes);

public sealed record ContactResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? CompanyId,
    string FullName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

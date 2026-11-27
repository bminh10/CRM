using System.Security.Claims;
using CRM.Api.Data;
using CRM.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/audit-logs")]
public sealed class AuditLogsController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid workspaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        if (workspaceId == Guid.Empty || page < 1 || pageSize is < 1 or > 200)
            return BadRequest(new { message = "Valid workspaceId, page and pageSize are required." });
        if (!await IsMember(workspaceId, userId, cancellationToken))
            return NotFound();
        var query = db.AuditLogs.AsNoTracking().Where(x => x.WorkspaceId == workspaceId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogResponse(x.Id, x.WorkspaceId, x.UserId,
                x.User == null ? null : x.User.FullName, x.Action, x.EntityType, x.EntityId,
                x.OldData, x.NewData, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, total });
    }

    [HttpGet("my-activity")]
    public async Task<IActionResult> GetMyActivity(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 200)
            return BadRequest(new { message = "Valid page and pageSize are required." });
        var query = db.AuditLogs.AsNoTracking().Where(x => x.UserId == userId && !x.Workspace.IsArchived);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogResponse(x.Id, x.WorkspaceId, x.UserId,
                x.User == null ? null : x.User.FullName, x.Action, x.EntityType, x.EntityId,
                x.OldData, x.NewData, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, total });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var item = await db.AuditLogs.AsNoTracking()
            .Where(x => x.Id == id && !x.Workspace.IsArchived
                && x.Workspace.WorkspaceMembers.Any(member => member.UserId == userId))
            .Select(x => new AuditLogResponse(x.Id, x.WorkspaceId, x.UserId,
                x.User == null ? null : x.User.FullName, x.Action, x.EntityType, x.EntityId,
                x.OldData, x.NewData, x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private Task<bool> IsMember(Guid workspaceId, Guid userId, CancellationToken ct) =>
        WorkspaceAccess.IsMemberAsync(db, workspaceId, userId, ct);
}

public sealed record AuditLogResponse(Guid Id, Guid WorkspaceId, Guid? UserId, string? UserName,
    string Action, string EntityType, Guid? EntityId, string? OldData, string? NewData, DateTime CreatedAt);

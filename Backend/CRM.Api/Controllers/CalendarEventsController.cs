using System.Security.Claims;
using CRM.Api.Data;
using CRM.Api.Models;
using CRM.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar-events")]
public sealed class CalendarEventsController(CrmDbContext db) : ControllerBase
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
        var query = db.CalendarEvents.AsNoTracking().Where(x => x.WorkspaceId == workspaceId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.StartTime).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ToResponseExpression())
            .ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, total });
    }

    [HttpGet("my-events")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var items = await db.CalendarEvents.AsNoTracking()
            .Where(x => !x.Workspace.IsArchived
                && x.Workspace.WorkspaceMembers.Any(member => member.UserId == userId)
                && (x.CreatedBy == userId || x.CalendarEventAttendees.Any(a => a.UserId == userId)))
            .OrderBy(x => x.StartTime).Select(ToResponseExpression()).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("range")]
    public async Task<IActionResult> GetRange(
        [FromQuery] Guid workspaceId,
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        if (workspaceId == Guid.Empty || end <= start)
            return BadRequest(new { message = "A workspace and valid start/end range are required." });
        if (!await IsMember(workspaceId, userId, cancellationToken))
            return NotFound();
        var items = await db.CalendarEvents.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && x.StartTime < end.UtcDateTime && x.EndTime >= start.UtcDateTime)
            .OrderBy(x => x.StartTime).Select(ToResponseExpression()).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var item = await db.CalendarEvents.AsNoTracking()
            .Where(x => x.Id == id && !x.Workspace.IsArchived
                && x.Workspace.WorkspaceMembers.Any(member => member.UserId == userId))
            .Select(ToResponseExpression()).SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCalendarEventRequest request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        if (request.WorkspaceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title)
            || request.EndTime <= request.StartTime)
            return BadRequest(new { message = "WorkspaceId, Title and an EndTime after StartTime are required." });
        if (!await CanEdit(request.WorkspaceId, userId, cancellationToken))
            return NotFound();
        if (!await ValidRelations(request.WorkspaceId, request.CompanyId, request.OpportunityId, cancellationToken))
            return BadRequest(new { message = "Related company and opportunity must belong to this workspace." });
        if (!await ValidAttendees(request.WorkspaceId, request.AttendeeUserIds, cancellationToken))
            return BadRequest(new { message = "Every attendee must be a workspace member." });

        var now = DateTime.UtcNow;
        var item = new CalendarEvent
        {
            Id = Guid.NewGuid(), WorkspaceId = request.WorkspaceId, CompanyId = request.CompanyId,
            OpportunityId = request.OpportunityId, Title = request.Title.Trim(), Description = request.Description,
            Location = request.Location, MeetingUrl = request.MeetingUrl, EventType = NormalizeEventType(request.EventType),
            StartTime = request.StartTime.UtcDateTime, EndTime = request.EndTime.UtcDateTime,
            CreatedBy = userId, CreatedAt = now, UpdatedAt = now
        };
        foreach (var attendeeId in request.AttendeeUserIds.Distinct())
            item.CalendarEventAttendees.Add(new CalendarEventAttendee
            {
                Id = Guid.NewGuid(), UserId = attendeeId, AttendanceStatus = "pending", InvitedAt = now
            });
        db.CalendarEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCalendarEventRequest request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var item = await db.CalendarEvents.Include(x => x.CalendarEventAttendees)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null || item.WorkspaceId != request.WorkspaceId
            || !await CanEdit(item.WorkspaceId, userId, cancellationToken))
            return NotFound();
        if (string.IsNullOrWhiteSpace(request.Title) || request.EndTime <= request.StartTime)
            return BadRequest(new { message = "Title is required and EndTime must follow StartTime." });
        if (!await ValidRelations(item.WorkspaceId, request.CompanyId, request.OpportunityId, cancellationToken))
            return BadRequest(new { message = "Related company and opportunity must belong to this workspace." });
        if (!await ValidAttendees(item.WorkspaceId, request.AttendeeUserIds, cancellationToken))
            return BadRequest(new { message = "Every attendee must be a workspace member." });

        item.CompanyId = request.CompanyId;
        item.OpportunityId = request.OpportunityId;
        item.Title = request.Title.Trim();
        item.Description = request.Description;
        item.Location = request.Location;
        item.MeetingUrl = request.MeetingUrl;
        item.EventType = NormalizeEventType(request.EventType);
        item.StartTime = request.StartTime.UtcDateTime;
        item.EndTime = request.EndTime.UtcDateTime;
        item.UpdatedAt = DateTime.UtcNow;
        db.CalendarEventAttendees.RemoveRange(item.CalendarEventAttendees);
        item.CalendarEventAttendees.Clear();
        foreach (var attendeeId in request.AttendeeUserIds.Distinct())
            item.CalendarEventAttendees.Add(new CalendarEventAttendee
            {
                Id = Guid.NewGuid(), EventId = item.Id, UserId = attendeeId,
                AttendanceStatus = "pending", InvitedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var item = await db.CalendarEvents.SingleOrDefaultAsync(
            x => x.Id == id && !x.Workspace.IsArchived
                && x.Workspace.WorkspaceMembers.Any(member => member.UserId == userId), cancellationToken);
        if (item is null)
            return NotFound();
        if (!await CanEdit(item.WorkspaceId, userId, cancellationToken))
            return NotFound();
        db.CalendarEvents.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private Task<bool> IsMember(Guid workspaceId, Guid userId, CancellationToken ct) =>
        WorkspaceAccess.IsMemberAsync(db, workspaceId, userId, ct);
    private Task<bool> CanEdit(Guid workspaceId, Guid userId, CancellationToken ct) =>
        WorkspaceAccess.CanEditAsync(db, workspaceId, userId, ct);

    private async Task<bool> ValidRelations(Guid workspaceId, Guid? companyId, Guid? opportunityId, CancellationToken ct)
    {
        if (companyId.HasValue && !await db.Companies.AnyAsync(x => x.Id == companyId && x.WorkspaceId == workspaceId, ct))
            return false;
        return !opportunityId.HasValue || await db.Opportunities.AnyAsync(
            x => x.Id == opportunityId && x.WorkspaceId == workspaceId, ct);
    }

    private async Task<bool> ValidAttendees(Guid workspaceId, List<Guid> ids, CancellationToken ct) =>
        await db.WorkspaceMembers.CountAsync(x => x.WorkspaceId == workspaceId && ids.Contains(x.UserId), ct)
        == ids.Distinct().Count();

    private static string NormalizeEventType(string type) => type.Trim().ToLowerInvariant() switch
    {
        "online" => "online",
        "phone" => "phone",
        _ => "offline"
    };

    private static System.Linq.Expressions.Expression<Func<CalendarEvent, CalendarEventResponse>> ToResponseExpression() =>
        x => new CalendarEventResponse(x.Id, x.WorkspaceId, x.CompanyId, x.OpportunityId, x.Title,
            x.Description, x.Location, x.MeetingUrl, x.EventType, x.StartTime, x.EndTime, x.CreatedBy,
            x.CreatedAt, x.UpdatedAt, x.CalendarEventAttendees.Select(a => a.UserId).ToList());

    private static CalendarEventResponse ToResponse(CalendarEvent x) => new(x.Id, x.WorkspaceId,
        x.CompanyId, x.OpportunityId, x.Title, x.Description, x.Location, x.MeetingUrl, x.EventType,
        x.StartTime, x.EndTime, x.CreatedBy, x.CreatedAt, x.UpdatedAt,
        x.CalendarEventAttendees.Select(a => a.UserId).ToList());
}

public sealed record CreateCalendarEventRequest(Guid WorkspaceId, Guid? CompanyId, Guid? OpportunityId,
    string Title, string? Description, string? Location, string? MeetingUrl, string EventType,
    DateTimeOffset StartTime, DateTimeOffset EndTime, List<Guid> AttendeeUserIds);
public sealed record UpdateCalendarEventRequest(Guid WorkspaceId, Guid? CompanyId, Guid? OpportunityId,
    string Title, string? Description, string? Location, string? MeetingUrl, string EventType,
    DateTimeOffset StartTime, DateTimeOffset EndTime, List<Guid> AttendeeUserIds);
public sealed record CalendarEventResponse(Guid Id, Guid WorkspaceId, Guid? CompanyId, Guid? OpportunityId,
    string Title, string? Description, string? Location, string? MeetingUrl, string EventType,
    DateTime StartTime, DateTime EndTime, Guid CreatedBy, DateTime CreatedAt, DateTime UpdatedAt,
    List<Guid> AttendeeUserIds);

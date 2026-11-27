using System.Security.Claims;
using CRM.Api.Data;
using CRM.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrmTask = CRM.Api.Models.Task;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(CrmDbContext db) : ControllerBase
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
        var query = db.Tasks.AsNoTracking().Where(x => x.WorkspaceId == workspaceId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.DueDate).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TaskResponse(x.Id, x.WorkspaceId, x.OpportunityId, x.Title, x.Description,
                x.Status, x.Priority, x.StartDate, x.DueDate, x.CreatedBy, x.CreatedAt, x.UpdatedAt,
                x.TaskAssignees.Select(a => a.UserId).ToList()))
            .ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, total });
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var items = await db.Tasks.AsNoTracking().Where(x =>
                x.Workspace.WorkspaceMembers.Any(member => member.UserId == userId)
                && x.TaskAssignees.Any(a => a.UserId == userId))
            .OrderBy(x => x.DueDate)
            .Select(x => new TaskResponse(x.Id, x.WorkspaceId, x.OpportunityId, x.Title, x.Description,
                x.Status, x.Priority, x.StartDate, x.DueDate, x.CreatedBy, x.CreatedAt, x.UpdatedAt,
                x.TaskAssignees.Select(a => a.UserId).ToList()))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        if (!await IsMember(workspaceId, userId, cancellationToken))
            return NotFound();
        var item = await db.Tasks.AsNoTracking()
            .Where(x => x.Id == id && x.WorkspaceId == workspaceId)
            .Select(x => new TaskResponse(x.Id, x.WorkspaceId, x.OpportunityId, x.Title, x.Description,
                x.Status, x.Priority, x.StartDate, x.DueDate, x.CreatedBy, x.CreatedAt, x.UpdatedAt,
                x.TaskAssignees.Select(a => a.UserId).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        if (request.WorkspaceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title)
            || request.StartDate.HasValue && request.DueDate.HasValue && request.StartDate > request.DueDate)
            return BadRequest(new { message = "Valid WorkspaceId and Title are required; StartDate cannot follow DueDate." });
        var status = request.Status is null ? "todo" : NormalizeStatus(request.Status);
        if (status is null)
            return BadRequest(new { message = "Status must be todo, in_progress, done or cancelled." });
        if (!await IsMember(request.WorkspaceId, userId, cancellationToken))
            return NotFound();
        if (request.OpportunityId.HasValue && !await db.Opportunities.AnyAsync(
                x => x.Id == request.OpportunityId && x.WorkspaceId == request.WorkspaceId, cancellationToken))
            return BadRequest(new { message = "Opportunity must belong to the specified workspace." });
        if (!await ValidAssignees(request.WorkspaceId, request.AssigneeUserIds, cancellationToken))
            return BadRequest(new { message = "Every assignee must be a workspace member." });
        var now = DateTime.UtcNow;
        var task = new CrmTask
        {
            Id = Guid.NewGuid(), WorkspaceId = request.WorkspaceId, OpportunityId = request.OpportunityId,
            Title = request.Title.Trim(), Description = request.Description, Status = status,
            Priority = NormalizePriority(request.Priority), StartDate = request.StartDate?.UtcDateTime,
            DueDate = request.DueDate?.UtcDateTime, CreatedBy = userId, CreatedAt = now, UpdatedAt = now
        };
        foreach (var assigneeId in request.AssigneeUserIds.Distinct())
            task.TaskAssignees.Add(new TaskAssignee
            {
                Id = Guid.NewGuid(), UserId = assigneeId, IsPrimary = task.TaskAssignees.Count == 0, AssignedAt = now
            });
        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = task.Id, workspaceId = task.WorkspaceId }, ToResponse(task));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var task = await db.Tasks.Include(x => x.TaskAssignees)
            .SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (task is null || !await IsMember(request.WorkspaceId, userId, cancellationToken))
            return NotFound();
        if (string.IsNullOrWhiteSpace(request.Title)
            || request.StartDate.HasValue && request.DueDate.HasValue && request.StartDate > request.DueDate)
            return BadRequest(new { message = "Valid Title is required; StartDate cannot follow DueDate." });
        var status = request.Status is null ? task.Status : NormalizeStatus(request.Status);
        if (status is null)
            return BadRequest(new { message = "Status must be todo, in_progress, done or cancelled." });
        if (request.OpportunityId.HasValue && !await db.Opportunities.AnyAsync(
                x => x.Id == request.OpportunityId && x.WorkspaceId == task.WorkspaceId, cancellationToken))
            return BadRequest(new { message = "Opportunity must belong to the specified workspace." });
        if (!await ValidAssignees(task.WorkspaceId, request.AssigneeUserIds, cancellationToken))
            return BadRequest(new { message = "Every assignee must be a workspace member." });
        task.OpportunityId = request.OpportunityId;
        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Status = status;
        task.Priority = NormalizePriority(request.Priority);
        task.StartDate = request.StartDate?.UtcDateTime;
        task.DueDate = request.DueDate?.UtcDateTime;
        task.UpdatedAt = DateTime.UtcNow;
        db.TaskAssignees.RemoveRange(task.TaskAssignees);
        task.TaskAssignees.Clear();
        foreach (var assigneeId in request.AssigneeUserIds.Distinct())
            task.TaskAssignees.Add(new TaskAssignee
            {
                Id = Guid.NewGuid(), TaskId = task.Id, UserId = assigneeId,
                IsPrimary = task.TaskAssignees.Count == 0, AssignedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(task));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var task = await db.Tasks.SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == workspaceId, cancellationToken);
        if (task is null || !await IsMember(workspaceId, userId, cancellationToken))
            return NotFound();
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        var status = NormalizeStatus(request.Status);
        if (status is null)
            return BadRequest(new { message = "Status must be todo, in_progress, done or cancelled." });
        var task = await db.Tasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null || !await IsMember(task.WorkspaceId, userId, cancellationToken))
            return NotFound();
        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { task.Id, task.WorkspaceId, task.Status, task.UpdatedAt });
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private Task<bool> IsMember(Guid workspaceId, Guid userId, CancellationToken ct) =>
        db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId, ct);
    private async Task<bool> ValidAssignees(Guid workspaceId, List<Guid> ids, CancellationToken ct) =>
        await db.WorkspaceMembers.CountAsync(x => x.WorkspaceId == workspaceId && ids.Distinct().Contains(x.UserId), ct)
        == ids.Distinct().Count();

    private static string NormalizePriority(string priority) => priority.Trim().ToLowerInvariant() switch
    {
        "low" => "low",
        "high" => "high",
        "urgent" => "urgent",
        _ => "medium"
    };

    private static string? NormalizeStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "todo" => "todo",
        "in_progress" => "in_progress",
        "done" => "done",
        "cancelled" => "cancelled",
        _ => null
    };

    private static TaskResponse ToResponse(CrmTask x) => new(x.Id, x.WorkspaceId, x.OpportunityId,
        x.Title, x.Description, x.Status, x.Priority, x.StartDate, x.DueDate, x.CreatedBy,
        x.CreatedAt, x.UpdatedAt, x.TaskAssignees.Select(a => a.UserId).ToList());
}

public sealed record CreateTaskRequest(Guid WorkspaceId, Guid? OpportunityId, string Title,
    string? Description, string Priority, DateTimeOffset? StartDate, DateTimeOffset? DueDate, List<Guid> AssigneeUserIds,
    string? Status = null);
public sealed record UpdateTaskRequest(Guid WorkspaceId, Guid? OpportunityId, string Title,
    string? Description, string Priority, DateTimeOffset? StartDate, DateTimeOffset? DueDate, List<Guid> AssigneeUserIds,
    string? Status = null);
public sealed record UpdateTaskStatusRequest(string Status);
public sealed record TaskResponse(Guid Id, Guid WorkspaceId, Guid? OpportunityId, string Title,
    string? Description, string Status, string Priority, DateTime? StartDate, DateTime? DueDate,
    Guid CreatedBy, DateTime CreatedAt, DateTime UpdatedAt, List<Guid> AssigneeUserIds);

using System.Security.Claims;
using System.Security.Cryptography;
using CRM.Api.Data;
using CRM.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces")]
public sealed class WorkspacesController(CrmDbContext db) : ControllerBase
{
    [HttpPost("personal")]
    public async Task<IActionResult> EnsurePersonalWorkspace(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM users WHERE id = {userId} FOR UPDATE", cancellationToken);
        if (!await db.Users.AnyAsync(x => x.Id == userId, cancellationToken))
            return Unauthorized();

        var existing = await db.Workspaces.AsNoTracking()
            .Where(x => x.WorkspaceMembers.Any(member => member.UserId == userId))
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return Ok(ToResponse(existing));
        }

        var now = DateTime.UtcNow;
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(), Name = "Không gian cá nhân", CreatedBy = userId,
            CreatedAt = now, UpdatedAt = now
        };
        db.Workspaces.Add(workspace);
        db.WorkspaceMembers.Add(new WorkspaceMember
        {
            Id = Guid.NewGuid(), WorkspaceId = workspace.Id, UserId = userId,
            Role = "owner", JoinedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = workspace.Id }, ToResponse(workspace));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });
        var now = DateTime.UtcNow;
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Description = request.Description,
            CreatedBy = userId, CreatedAt = now, UpdatedAt = now
        };
        db.Workspaces.Add(workspace);
        db.WorkspaceMembers.Add(new WorkspaceMember
        {
            Id = Guid.NewGuid(), WorkspaceId = workspace.Id, UserId = userId, Role = "owner", JoinedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = workspace.Id }, ToResponse(workspace));
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var items = await db.Workspaces.AsNoTracking()
            .Where(x => x.WorkspaceMembers.Any(member => member.UserId == userId))
            .OrderBy(x => x.Name)
            .Select(x => new WorkspaceResponse(x.Id, x.Name, x.Description, x.CreatedBy, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var workspace = await db.Workspaces.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceMembers.Any(member => member.UserId == userId), cancellationToken);
        return workspace is null ? NotFound() : Ok(ToResponse(workspace));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });
        if (!await IsWorkspaceAdmin(id, userId, cancellationToken))
            return NotFound();
        var workspace = await db.Workspaces.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workspace is null)
            return NotFound();
        workspace.Name = request.Name.Trim();
        workspace.Description = request.Description;
        workspace.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(workspace));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await IsWorkspaceAdmin(id, userId, cancellationToken))
            return NotFound();
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Workspace deletion needs an archival/cascade-retention policy; dependent CRM records are not automatically deleted."
        });
    }

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await IsMember(id, userId, cancellationToken))
            return NotFound();
        var items = await db.WorkspaceMembers.AsNoTracking().Where(x => x.WorkspaceId == id)
            .OrderBy(x => x.User.FullName)
            .Select(x => new WorkspaceMemberResponse(x.UserId, x.User.Email, x.User.FullName, x.User.AvatarUrl, x.Role, x.JoinedAt))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}/statistics")]
    public async Task<IActionResult> Statistics(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await IsMember(id, userId, cancellationToken))
            return NotFound();
        return Ok(new
        {
            members = await db.WorkspaceMembers.CountAsync(x => x.WorkspaceId == id, cancellationToken),
            companies = await db.Companies.CountAsync(x => x.WorkspaceId == id, cancellationToken),
            contacts = await db.Contacts.CountAsync(x => x.WorkspaceId == id, cancellationToken),
            pipelines = await db.Pipelines.CountAsync(x => x.WorkspaceId == id, cancellationToken),
            opportunities = await db.Opportunities.CountAsync(x => x.WorkspaceId == id, cancellationToken),
            tasks = await db.Tasks.CountAsync(x => x.WorkspaceId == id, cancellationToken),
            events = await db.CalendarEvents.CountAsync(x => x.WorkspaceId == id, cancellationToken)
        });
    }

    [HttpPost("{workspaceId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid workspaceId, AddWorkspaceMemberRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();
        if (!await IsWorkspaceAdmin(workspaceId, currentUserId, cancellationToken))
            return NotFound();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null)
            return NotFound();
        if (await db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == request.UserId, cancellationToken))
            return Conflict(new { message = "User is already a workspace member." });
        var role = NormalizeRole(request.Role);
        if (role is null)
            return BadRequest(new { message = "Role must be owner, editor or viewer." });
        if (role == "owner")
            return BadRequest(new { message = "Use the role endpoint to transfer ownership; a new member cannot be assigned as owner directly." });
        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(), WorkspaceId = workspaceId, UserId = request.UserId,
            Role = role, JoinedAt = DateTime.UtcNow
        };
        db.WorkspaceMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/workspaces/{workspaceId}/members/{user.Id}",
            new WorkspaceMemberResponse(user.Id, user.Email, user.FullName, user.AvatarUrl, member.Role, member.JoinedAt));
    }

    [HttpGet("{workspaceId:guid}/members/me")]
    public async Task<IActionResult> GetMyMembership(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var item = await db.WorkspaceMembers.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && x.UserId == userId)
            .Select(x => new WorkspaceMemberResponse(x.UserId, x.User.Email, x.User.FullName, x.User.AvatarUrl, x.Role, x.JoinedAt))
            .SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{workspaceId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> GetMember(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();
        if (!await IsMember(workspaceId, currentUserId, cancellationToken))
            return NotFound();
        var member = await db.WorkspaceMembers.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && x.UserId == userId)
            .Select(x => new WorkspaceMemberResponse(x.UserId, x.User.Email, x.User.FullName, x.User.AvatarUrl, x.Role, x.JoinedAt))
            .SingleOrDefaultAsync(cancellationToken);
        return member is null ? NotFound() : Ok(member);
    }

    [HttpPut("{workspaceId:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid workspaceId, Guid userId, UpdateWorkspaceMemberRoleRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();
        if (!await IsWorkspaceAdmin(workspaceId, currentUserId, cancellationToken))
            return NotFound();
        var role = NormalizeRole(request.Role);
        if (role is null)
            return BadRequest(new { message = "Role must be owner, editor or viewer." });
        var callerRole = await db.WorkspaceMembers
            .Where(x => x.WorkspaceId == workspaceId && x.UserId == currentUserId)
            .Select(x => x.Role).SingleOrDefaultAsync(cancellationToken);
        var member = await db.WorkspaceMembers.SingleOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId, cancellationToken);
        if (member is null)
            return NotFound();
        if (member.Role == "owner" && callerRole != "owner")
            return Forbid();
        if (role == "owner")
        {
            if (callerRole != "owner")
                return Forbid();
            var oldOwner = await db.WorkspaceMembers.SingleAsync(
                x => x.WorkspaceId == workspaceId && x.Role == "owner", cancellationToken);
            oldOwner.Role = "editor";
        }
        else if (member.Role == "owner")
            return Conflict(new { message = "Transfer ownership by assigning the owner role to another member." });
        member.Role = role;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { userId, workspaceId, role });
    }

    [HttpDelete("{workspaceId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();
        var isSelf = currentUserId == userId;
        if (!isSelf && !await IsWorkspaceAdmin(workspaceId, currentUserId, cancellationToken))
            return NotFound();
        var member = await db.WorkspaceMembers.SingleOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId, cancellationToken);
        if (member is null)
            return NotFound();
        if (member.Role == "owner")
            return Conflict(new { message = "Transfer workspace ownership before removing its owner." });
        db.WorkspaceMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("{workspaceId:guid}/members/{userId:guid}/status")]
    public IActionResult UpdateMemberStatus(Guid workspaceId, Guid userId) =>
        StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Workspace member status requires a status column and migration; membership removal is available instead."
        });

    [HttpPost("{workspaceId:guid}/invitations")]
    public async Task<IActionResult> CreateInvitation(Guid workspaceId, CreateInvitationRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await IsWorkspaceAdmin(workspaceId, userId, cancellationToken))
            return NotFound();
        var email = request.Email.Trim().ToLowerInvariant();
        if (!email.Contains('@'))
            return BadRequest(new { message = "A valid email is required." });
        var invitation = new WorkspaceInvitation
        {
            Id = Guid.NewGuid(), WorkspaceId = workspaceId, InvitedBy = userId,
            Email = email, Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
            Status = "pending", ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow
        };
        db.WorkspaceInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetInvitations), new { workspaceId },
            new InvitationResponse(invitation.Id, invitation.WorkspaceId, invitation.Email,
                invitation.Status, invitation.ExpiresAt, invitation.CreatedAt, invitation.Token));
    }

    [HttpGet("{workspaceId:guid}/invitations")]
    public async Task<IActionResult> GetInvitations(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await IsWorkspaceAdmin(workspaceId, userId, cancellationToken))
            return NotFound();
        var items = await db.WorkspaceInvitations.AsNoTracking().Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new InvitationResponse(x.Id, x.WorkspaceId, x.Email, x.Status, x.ExpiresAt, x.CreatedAt, null))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("/api/invitations")]
    public async Task<IActionResult> GetIncomingInvitations(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
            return Unauthorized();
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return Forbid();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var items = await db.WorkspaceInvitations.AsNoTracking()
            .Where(x => x.Email == normalizedEmail && x.Status == "pending" && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new IncomingInvitationResponse(x.Id, x.WorkspaceId, x.Workspace.Name,
                x.Email, x.InvitedByNavigation.FullName, x.ExpiresAt, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("/api/invitations/{id:guid}/accept")]
    public Task<IActionResult> AcceptInvitationById(Guid id, CancellationToken cancellationToken) =>
        RespondInvitationById(id, "accepted", cancellationToken);

    [HttpPost("/api/invitations/{id:guid}/reject")]
    public Task<IActionResult> RejectInvitationById(Guid id, CancellationToken cancellationToken) =>
        RespondInvitationById(id, "rejected", cancellationToken);

    [HttpPost("/api/invitations/{token}/accept")]
    public Task<IActionResult> AcceptInvitation(string token, CancellationToken cancellationToken) =>
        RespondInvitation(token, "accepted", cancellationToken);

    [HttpPost("/api/invitations/{token}/reject")]
    public Task<IActionResult> RejectInvitation(string token, CancellationToken cancellationToken) =>
        RespondInvitation(token, "rejected", cancellationToken);

    [HttpDelete("/api/invitations/{id:guid}")]
    public async Task<IActionResult> DeleteInvitation(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var invitation = await db.WorkspaceInvitations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invitation is null || !await IsWorkspaceAdmin(invitation.WorkspaceId, userId, cancellationToken))
            return NotFound();
        db.WorkspaceInvitations.Remove(invitation);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> RespondInvitationById(Guid id, string status, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return Forbid();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM workspace_invitations WHERE id = {id} FOR UPDATE", cancellationToken);
        var invitation = await db.WorkspaceInvitations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invitation is null || invitation.Status != "pending" || invitation.ExpiresAt <= DateTime.UtcNow)
            return NotFound();
        if (!string.Equals(invitation.Email, email, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM users WHERE id = {userId} FOR UPDATE", cancellationToken);
        if (!await db.Users.AnyAsync(x => x.Id == userId, cancellationToken))
            return Unauthorized();

        invitation.Status = status;
        if (status == "accepted" && !await db.WorkspaceMembers.AnyAsync(
                x => x.WorkspaceId == invitation.WorkspaceId && x.UserId == userId, cancellationToken))
        {
            db.WorkspaceMembers.Add(new WorkspaceMember
            {
                Id = Guid.NewGuid(), WorkspaceId = invitation.WorkspaceId, UserId = userId,
                Role = "editor", JoinedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { invitationId = invitation.Id, status = invitation.Status });
    }

    private async Task<IActionResult> RespondInvitation(string token, string status, CancellationToken cancellationToken)
    {
        var invitationId = await db.WorkspaceInvitations.AsNoTracking()
            .Where(x => x.Token == token)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return invitationId is null
            ? NotFound()
            : await RespondInvitationById(invitationId.Value, status, cancellationToken);
    }

    private bool TryGetCurrentUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private Task<bool> IsMember(Guid workspaceId, Guid userId, CancellationToken ct) =>
        db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId, ct);

    private Task<bool> IsWorkspaceAdmin(Guid workspaceId, Guid userId, CancellationToken ct) =>
        db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId
            && x.Role == "owner", ct);

    private static string? NormalizeRole(string role) => role.Trim().ToLowerInvariant() switch
    {
        "owner" => "owner",
        "editor" => "editor",
        "viewer" => "viewer",
        _ => null
    };

    private static WorkspaceResponse ToResponse(Workspace x) =>
        new(x.Id, x.Name, x.Description, x.CreatedBy, x.CreatedAt, x.UpdatedAt);
}

public sealed record CreateWorkspaceRequest(string Name, string? Description);
public sealed record UpdateWorkspaceRequest(string Name, string? Description);
public sealed record WorkspaceResponse(Guid Id, string Name, string? Description, Guid CreatedBy, DateTime? CreatedAt, DateTime? UpdatedAt);
public sealed record WorkspaceMemberResponse(Guid UserId, string Email, string FullName, string? AvatarUrl, string Role, DateTime? JoinedAt);
public sealed record AddWorkspaceMemberRequest(Guid UserId, string Role);
public sealed record UpdateWorkspaceMemberRoleRequest(string Role);
public sealed record CreateInvitationRequest(string Email);
public sealed record InvitationResponse(Guid Id, Guid WorkspaceId, string Email, string Status, DateTime ExpiresAt, DateTime? CreatedAt, string? Token);
public sealed record IncomingInvitationResponse(Guid Id, Guid WorkspaceId, string WorkspaceName,
    string Email, string InvitedByName, DateTime ExpiresAt, DateTime? CreatedAt);

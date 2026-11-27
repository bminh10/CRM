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
[Route("api/opportunities")]
public sealed class OpportunitiesController : ControllerBase
{
    private readonly CrmDbContext _db;

    public OpportunitiesController(CrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            return BadRequest(new { message = "WorkspaceId is required." });
        if (!await IsMember(workspaceId))
            return NotFound();
        var items = await _db.Opportunities.AsNoTracking().Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new OpportunityResponse(x.Id, x.WorkspaceId, x.PipelineId, x.StageId,
                x.CompanyId, x.ContactId, x.Title, x.Description, x.ExpectedValue,
                x.Probability, x.ExpectedCloseDate, x.Status, x.CreatedAt, x.UpdatedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid workspaceId)
    {
        if (!await IsMember(workspaceId))
            return NotFound();
        var item = await _db.Opportunities.AsNoTracking()
            .Where(x => x.Id == id && x.WorkspaceId == workspaceId)
            .Select(x => new OpportunityResponse(x.Id, x.WorkspaceId, x.PipelineId, x.StageId,
                x.CompanyId, x.ContactId, x.Title, x.Description, x.ExpectedValue,
                x.Probability, x.ExpectedCloseDate, x.Status, x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(OpportunityRequest request)
    {
        if (request.WorkspaceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title)
            || request.Probability is < 0 or > 100 || request.ExpectedValue < 0)
            return BadRequest(new { message = "WorkspaceId, Title, a non-negative value and probability from 0 to 100 are required." });
        if (!await CanEdit(request.WorkspaceId))
            return NotFound();
        if (!await _db.Pipelines.AnyAsync(x => x.Id == request.PipelineId && x.WorkspaceId == request.WorkspaceId)
            || !await _db.PipelineStages.AnyAsync(x => x.Id == request.StageId && x.PipelineId == request.PipelineId))
            return BadRequest(new { message = "Pipeline and stage must belong to the specified workspace." });
        if (request.CompanyId.HasValue && !await _db.Companies.AnyAsync(
                x => x.Id == request.CompanyId && x.WorkspaceId == request.WorkspaceId))
            return BadRequest(new { message = "Company must belong to the specified workspace." });
        if (request.ContactId.HasValue && !await _db.Contacts.AnyAsync(
                x => x.Id == request.ContactId && x.WorkspaceId == request.WorkspaceId))
            return BadRequest(new { message = "Contact must belong to the specified workspace." });

        var now = DateTime.UtcNow;
        var item = new Opportunity
        {
            Id = Guid.NewGuid(), WorkspaceId = request.WorkspaceId, PipelineId = request.PipelineId,
            StageId = request.StageId, CompanyId = request.CompanyId, ContactId = request.ContactId,
            Title = request.Title.Trim(), Description = request.Description, ExpectedValue = request.ExpectedValue,
            Probability = request.Probability, ExpectedCloseDate = request.ExpectedCloseDate,
            Status = "open", CreatedAt = now, UpdatedAt = now
        };
        _db.Opportunities.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id, workspaceId = item.WorkspaceId }, ToResponse(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, OpportunityRequest request)
    {
        var item = await _db.Opportunities.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();
        if (!await CanEdit(item.WorkspaceId))
            return NotFound();
        if (item.WorkspaceId != request.WorkspaceId)
            return BadRequest(new { message = "WorkspaceId cannot be changed." });
        if (string.IsNullOrWhiteSpace(request.Title) || request.Probability is < 0 or > 100 || request.ExpectedValue < 0)
            return BadRequest(new { message = "Title, a non-negative value and probability from 0 to 100 are required." });
        if (!await _db.Pipelines.AnyAsync(x => x.Id == request.PipelineId && x.WorkspaceId == item.WorkspaceId)
            || !await _db.PipelineStages.AnyAsync(x => x.Id == request.StageId && x.PipelineId == request.PipelineId))
            return BadRequest(new { message = "Pipeline and stage must belong to the specified workspace." });
        if (request.CompanyId.HasValue && !await _db.Companies.AnyAsync(
                x => x.Id == request.CompanyId && x.WorkspaceId == item.WorkspaceId))
            return BadRequest(new { message = "Company must belong to the specified workspace." });
        if (request.ContactId.HasValue && !await _db.Contacts.AnyAsync(
                x => x.Id == request.ContactId && x.WorkspaceId == item.WorkspaceId))
            return BadRequest(new { message = "Contact must belong to the specified workspace." });
        item.PipelineId = request.PipelineId;
        item.StageId = request.StageId;
        item.CompanyId = request.CompanyId;
        item.ContactId = request.ContactId;
        item.Title = request.Title.Trim();
        item.Description = request.Description;
        item.ExpectedValue = request.ExpectedValue;
        item.Probability = request.Probability;
        item.ExpectedCloseDate = request.ExpectedCloseDate;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Opportunities.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();
        if (!await CanEdit(item.WorkspaceId))
            return NotFound();
        _db.Opportunities.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/stage")]
    public async Task<IActionResult> UpdateStage(Guid id, UpdateOpportunityStageRequest request)
    {
        var item = await _db.Opportunities.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();
        if (!await CanEdit(item.WorkspaceId))
            return NotFound();
        if (!await _db.PipelineStages.AnyAsync(x => x.Id == request.StageId && x.PipelineId == item.PipelineId))
            return BadRequest(new { message = "Stage must belong to the opportunity pipeline." });
        item.StageId = request.StageId;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(item));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateOpportunityStatusRequest request)
    {
        var status = request.Status.Trim().ToLowerInvariant();
        if (status is not ("open" or "won" or "lost"))
            return BadRequest(new { message = "Status must be open, won or lost." });
        var item = await _db.Opportunities.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();
        if (!await CanEdit(item.WorkspaceId))
            return NotFound();
        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(item));
    }

    [HttpGet("/api/opportunities/statistics")]
    public async Task<IActionResult> Statistics([FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            return BadRequest(new { message = "WorkspaceId is required." });
        if (!await IsMember(workspaceId))
            return NotFound();
        var query = _db.Opportunities.AsNoTracking().Where(x => x.WorkspaceId == workspaceId);
        return Ok(new
        {
            total = await query.CountAsync(),
            open = await query.CountAsync(x => x.Status == "open"),
            won = await query.CountAsync(x => x.Status == "won"),
            lost = await query.CountAsync(x => x.Status == "lost"),
            totalExpectedValue = await query.Where(x => x.Status == "open").SumAsync(x => (decimal?)x.ExpectedValue) ?? 0
        });
    }

    [HttpGet("{opportunityId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid opportunityId)
    {
        var opportunity = await _db.Opportunities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == opportunityId);
        if (opportunity is null || !await IsMember(opportunity.WorkspaceId))
            return NotFound();
        var items = await _db.OpportunityMembers.AsNoTracking()
            .Where(x => x.OpportunityId == opportunityId)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.User.FullName)
            .Select(x => new OpportunityMemberResponse(x.UserId, x.User.Email, x.User.FullName, x.IsPrimary, x.AssignedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("{opportunityId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid opportunityId, AddOpportunityMemberRequest request)
    {
        var opportunity = await _db.Opportunities.SingleOrDefaultAsync(x => x.Id == opportunityId);
        if (opportunity is null || !await CanEdit(opportunity.WorkspaceId))
            return NotFound();
        if (!await _db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == opportunity.WorkspaceId && x.UserId == request.UserId))
            return BadRequest(new { message = "User must be a member of the opportunity workspace." });
        if (await _db.OpportunityMembers.AnyAsync(x => x.OpportunityId == opportunityId && x.UserId == request.UserId))
            return Conflict(new { message = "User is already assigned to this opportunity." });
        if (request.IsPrimary)
        {
            var existing = await _db.OpportunityMembers.Where(x => x.OpportunityId == opportunityId && x.IsPrimary).ToListAsync();
            foreach (var member in existing)
                member.IsPrimary = false;
        }
        var memberToAdd = new OpportunityMember
        {
            Id = Guid.NewGuid(), OpportunityId = opportunityId, UserId = request.UserId,
            IsPrimary = request.IsPrimary, AssignedAt = DateTime.UtcNow
        };
        _db.OpportunityMembers.Add(memberToAdd);
        await _db.SaveChangesAsync();
        var user = await _db.Users.AsNoTracking().SingleAsync(x => x.Id == request.UserId);
        return CreatedAtAction(nameof(GetMembers), new { opportunityId },
            new OpportunityMemberResponse(user.Id, user.Email, user.FullName, memberToAdd.IsPrimary, memberToAdd.AssignedAt));
    }

    [HttpPut("{opportunityId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> UpdateMember(Guid opportunityId, Guid userId, UpdateOpportunityMemberRequest request)
    {
        var opportunity = await _db.Opportunities.SingleOrDefaultAsync(x => x.Id == opportunityId);
        if (opportunity is null || !await CanEdit(opportunity.WorkspaceId))
            return NotFound();
        var member = await _db.OpportunityMembers.SingleOrDefaultAsync(x =>
            x.OpportunityId == opportunityId && x.UserId == userId);
        if (member is null)
            return NotFound();
        if (request.IsPrimary)
        {
            var existing = await _db.OpportunityMembers
                .Where(x => x.OpportunityId == opportunityId && x.IsPrimary && x.UserId != userId).ToListAsync();
            foreach (var primary in existing)
                primary.IsPrimary = false;
        }
        member.IsPrimary = request.IsPrimary;
        await _db.SaveChangesAsync();
        var user = await _db.Users.AsNoTracking().SingleAsync(x => x.Id == userId);
        return Ok(new OpportunityMemberResponse(user.Id, user.Email, user.FullName, member.IsPrimary, member.AssignedAt));
    }

    [HttpDelete("{opportunityId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid opportunityId, Guid userId)
    {
        var opportunity = await _db.Opportunities.SingleOrDefaultAsync(x => x.Id == opportunityId);
        if (opportunity is null || !await CanEdit(opportunity.WorkspaceId))
            return NotFound();
        var member = await _db.OpportunityMembers.SingleOrDefaultAsync(x =>
            x.OpportunityId == opportunityId && x.UserId == userId);
        if (member is null)
            return NotFound();
        _db.OpportunityMembers.Remove(member);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static OpportunityResponse ToResponse(Opportunity x) =>
        new(x.Id, x.WorkspaceId, x.PipelineId, x.StageId, x.CompanyId, x.ContactId, x.Title,
            x.Description, x.ExpectedValue, x.Probability, x.ExpectedCloseDate, x.Status, x.CreatedAt, x.UpdatedAt);

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

public sealed record OpportunityRequest(Guid WorkspaceId, Guid PipelineId, Guid StageId,
    Guid? CompanyId, Guid? ContactId, string Title, string? Description,
    decimal ExpectedValue, int Probability, DateOnly? ExpectedCloseDate);
public sealed record UpdateOpportunityStageRequest(Guid StageId);
public sealed record UpdateOpportunityStatusRequest(string Status);
public sealed record AddOpportunityMemberRequest(Guid UserId, bool IsPrimary);
public sealed record UpdateOpportunityMemberRequest(bool IsPrimary);
public sealed record OpportunityMemberResponse(Guid UserId, string Email, string FullName, bool IsPrimary, DateTime AssignedAt);
public sealed record OpportunityResponse(Guid Id, Guid WorkspaceId, Guid PipelineId, Guid StageId,
    Guid? CompanyId, Guid? ContactId, string Title, string? Description, decimal ExpectedValue,
    int Probability, DateOnly? ExpectedCloseDate, string Status, DateTime CreatedAt, DateTime UpdatedAt);

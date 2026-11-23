using CRM.Api.Data;
using CRM.Api.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pipelines")]
public sealed class PipelinesController : ControllerBase
{
    private readonly CrmDbContext _db;

    public PipelinesController(CrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            return BadRequest(new { message = "WorkspaceId is required." });
        if (!await IsMember(workspaceId))
            return NotFound();
        var items = await _db.Pipelines.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderBy(x => x.Name)
            .Select(x => new PipelineResponse(x.Id, x.WorkspaceId, x.Name, x.Description,
                x.IsDefault, x.CreatedAt, x.UpdatedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid workspaceId)
    {
        if (!await IsMember(workspaceId))
            return NotFound();
        var item = await _db.Pipelines.AsNoTracking()
            .Where(x => x.Id == id && x.WorkspaceId == workspaceId)
            .Select(x => new PipelineResponse(x.Id, x.WorkspaceId, x.Name, x.Description,
                x.IsDefault, x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PipelineRequest request)
    {
        if (request.WorkspaceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "WorkspaceId and Name are required." });
        if (!await IsMember(request.WorkspaceId))
            return NotFound();

        var now = DateTime.UtcNow;
        var pipeline = new Pipeline
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            Name = request.Name.Trim(),
            Description = request.Description,
            IsDefault = request.IsDefault,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Pipelines.Add(pipeline);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = pipeline.Id, workspaceId = pipeline.WorkspaceId },
            new PipelineResponse(pipeline.Id, pipeline.WorkspaceId, pipeline.Name, pipeline.Description,
                pipeline.IsDefault, pipeline.CreatedAt, pipeline.UpdatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PipelineRequest request)
    {
        var pipeline = await _db.Pipelines.SingleOrDefaultAsync(x => x.Id == id);
        if (pipeline is null)
            return NotFound();
        if (!await IsMember(pipeline.WorkspaceId))
            return NotFound();
        if (pipeline.WorkspaceId != request.WorkspaceId)
            return BadRequest(new { message = "WorkspaceId cannot be changed." });
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });
        pipeline.Name = request.Name.Trim();
        pipeline.Description = request.Description;
        pipeline.IsDefault = request.IsDefault;
        pipeline.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new PipelineResponse(pipeline.Id, pipeline.WorkspaceId, pipeline.Name,
            pipeline.Description, pipeline.IsDefault, pipeline.CreatedAt, pipeline.UpdatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var pipeline = await _db.Pipelines.SingleOrDefaultAsync(x => x.Id == id);
        if (pipeline is null)
            return NotFound();
        if (!await IsMember(pipeline.WorkspaceId))
            return NotFound();
        if (await _db.Opportunities.AnyAsync(x => x.PipelineId == id))
            return Conflict(new { message = "Pipeline cannot be deleted while it contains opportunities." });
        _db.Pipelines.Remove(pipeline);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{pipelineId:guid}/stages")]
    public async Task<IActionResult> GetStages(Guid pipelineId)
    {
        var pipelineWorkspaceId = await _db.Pipelines.Where(x => x.Id == pipelineId)
            .Select(x => (Guid?)x.WorkspaceId).SingleOrDefaultAsync();
        if (!pipelineWorkspaceId.HasValue || !await IsMember(pipelineWorkspaceId.Value))
            return NotFound();
        var items = await _db.PipelineStages.AsNoTracking()
            .Where(x => x.PipelineId == pipelineId)
            .OrderBy(x => x.Position)
            .Select(x => new PipelineStageResponse(x.Id, x.PipelineId, x.Name, x.Position, x.StageType, x.CreatedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("{pipelineId:guid}/stages")]
    public async Task<IActionResult> CreateStage(Guid pipelineId, PipelineStageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Position < 0)
            return BadRequest(new { message = "Name and a non-negative Position are required." });
        var pipelineWorkspaceId = await _db.Pipelines.Where(x => x.Id == pipelineId)
            .Select(x => (Guid?)x.WorkspaceId).SingleOrDefaultAsync();
        if (!pipelineWorkspaceId.HasValue || !await IsMember(pipelineWorkspaceId.Value))
            return NotFound();
        if (await _db.PipelineStages.AnyAsync(x => x.PipelineId == pipelineId && x.Position == request.Position))
            return Conflict(new { message = "Position is already used in this pipeline." });
        var stage = new PipelineStage
        {
            Id = Guid.NewGuid(),
            PipelineId = pipelineId,
            Name = request.Name.Trim(),
            Position = request.Position,
            StageType = request.StageType,
            CreatedAt = DateTime.UtcNow
        };
        _db.PipelineStages.Add(stage);
        await _db.SaveChangesAsync();
        return Created($"/api/pipeline-stages/{stage.Id}",
            new PipelineStageResponse(stage.Id, stage.PipelineId, stage.Name, stage.Position, stage.StageType, stage.CreatedAt));
    }

    [HttpPatch("{pipelineId:guid}/stages/reorder")]
    public async Task<IActionResult> ReorderStages(Guid pipelineId, ReorderPipelineStagesRequest request)
    {
        var pipelineWorkspaceId = await _db.Pipelines.Where(x => x.Id == pipelineId)
            .Select(x => (Guid?)x.WorkspaceId).SingleOrDefaultAsync();
        if (!pipelineWorkspaceId.HasValue || !await IsMember(pipelineWorkspaceId.Value))
            return NotFound();
        if (request.StageIds.Count == 0 || request.StageIds.Distinct().Count() != request.StageIds.Count)
            return BadRequest(new { message = "StageIds must be non-empty and unique." });
        var stages = await _db.PipelineStages.Where(x => x.PipelineId == pipelineId).ToListAsync();
        if (stages.Count != request.StageIds.Count || stages.Any(s => !request.StageIds.Contains(s.Id)))
            return BadRequest(new { message = "StageIds must contain every stage in the pipeline exactly once." });

        await using var transaction = await _db.Database.BeginTransactionAsync();
        for (var i = 0; i < stages.Count; i++)
            stages[i].Position = -i - 1;
        await _db.SaveChangesAsync();
        for (var i = 0; i < request.StageIds.Count; i++)
            stages.Single(x => x.Id == request.StageIds[i]).Position = i;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }

    private System.Threading.Tasks.Task<bool> IsMember(Guid workspaceId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return System.Threading.Tasks.Task.FromResult(false);
        return _db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId);
    }
}

[ApiController]
[Authorize]
[Route("api/pipeline-stages")]
public sealed class PipelineStagesController : ControllerBase
{
    private readonly CrmDbContext _db;

    public PipelineStagesController(CrmDbContext db) => _db = db;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await _db.PipelineStages.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new PipelineStageResponse(x.Id, x.PipelineId, x.Name, x.Position, x.StageType, x.CreatedAt))
            .SingleOrDefaultAsync();
        return item is null || !await IsMember(item.PipelineId) ? NotFound() : Ok(item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PipelineStageUpdateRequest request)
    {
        var stage = await _db.PipelineStages.SingleOrDefaultAsync(x => x.Id == id);
        if (stage is null)
            return NotFound();
        if (!await IsMember(stage.PipelineId))
            return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Position < 0)
            return BadRequest(new { message = "Name and a non-negative Position are required." });
        if (await _db.PipelineStages.AnyAsync(x => x.PipelineId == stage.PipelineId
                && x.Id != id && x.Position == request.Position))
            return Conflict(new { message = "Position is already used in this pipeline." });
        stage.Name = request.Name.Trim();
        stage.Position = request.Position;
        stage.StageType = request.StageType;
        await _db.SaveChangesAsync();
        return Ok(new PipelineStageResponse(stage.Id, stage.PipelineId, stage.Name,
            stage.Position, stage.StageType, stage.CreatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var stage = await _db.PipelineStages.SingleOrDefaultAsync(x => x.Id == id);
        if (stage is null)
            return NotFound();
        if (!await IsMember(stage.PipelineId))
            return NotFound();
        if (await _db.Opportunities.AnyAsync(x => x.StageId == id))
            return Conflict(new { message = "Stage cannot be deleted while it contains opportunities." });
        _db.PipelineStages.Remove(stage);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> IsMember(Guid pipelineId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return false;
        return await _db.Pipelines.AnyAsync(x => x.Id == pipelineId
            && x.Workspace.WorkspaceMembers.Any(member => member.UserId == userId));
    }
}

public sealed record PipelineRequest(Guid WorkspaceId, string Name, string? Description, bool IsDefault);
public sealed record PipelineResponse(Guid Id, Guid WorkspaceId, string Name, string? Description,
    bool IsDefault, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record PipelineStageRequest(string Name, int Position, string StageType);
public sealed record PipelineStageUpdateRequest(string Name, int Position, string StageType);
public sealed record ReorderPipelineStagesRequest(List<Guid> StageIds);
public sealed record PipelineStageResponse(Guid Id, Guid PipelineId, string Name, int Position,
    string StageType, DateTime CreatedAt);

using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class Pipeline
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    public virtual ICollection<PipelineStage> PipelineStages { get; set; } = new List<PipelineStage>();

    public virtual Workspace Workspace { get; set; } = null!;
}

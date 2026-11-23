using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class PipelineStage
{
    public Guid Id { get; set; }

    public Guid PipelineId { get; set; }

    public string Name { get; set; } = null!;

    public int Position { get; set; }

    public string StageType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    public virtual Pipeline Pipeline { get; set; } = null!;
}

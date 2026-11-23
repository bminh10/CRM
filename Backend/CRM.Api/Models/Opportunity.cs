using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class Opportunity
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid PipelineId { get; set; }

    public Guid StageId { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? ContactId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal ExpectedValue { get; set; }

    public int Probability { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual Company? Company { get; set; }

    public virtual Contact? Contact { get; set; }

    public virtual ICollection<OpportunityMember> OpportunityMembers { get; set; } = new List<OpportunityMember>();

    public virtual Pipeline Pipeline { get; set; } = null!;

    public virtual PipelineStage Stage { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual Workspace Workspace { get; set; } = null!;
}

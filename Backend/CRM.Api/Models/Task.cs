using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class Task
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? OpportunityId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Opportunity? Opportunity { get; set; }

    public virtual ICollection<TaskAssignee> TaskAssignees { get; set; } = new List<TaskAssignee>();

    public virtual Workspace Workspace { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class Workspace
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsArchived { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    public virtual ICollection<Pipeline> Pipelines { get; set; } = new List<Pipeline>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<WorkspaceInvitation> WorkspaceInvitations { get; set; } = new List<WorkspaceInvitation>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}

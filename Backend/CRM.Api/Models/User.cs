using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public bool? IsVerified { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public string? OtpSecret { get; set; }

    public string? OtpPendingSecret { get; set; }

    public DateTime? OtpPendingExpiresAt { get; set; }

    public long? OtpLastUsedStep { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CalendarEventAttendee> CalendarEventAttendees { get; set; } = new List<CalendarEventAttendee>();

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual ICollection<OpportunityMember> OpportunityMembers { get; set; } = new List<OpportunityMember>();

    public virtual ICollection<TaskAssignee> TaskAssignees { get; set; } = new List<TaskAssignee>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<WorkspaceInvitation> WorkspaceInvitations { get; set; } = new List<WorkspaceInvitation>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();

    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
}

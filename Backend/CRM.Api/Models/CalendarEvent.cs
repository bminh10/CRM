using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class CalendarEvent
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? OpportunityId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public string? MeetingUrl { get; set; }

    public string EventType { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CalendarEventAttendee> CalendarEventAttendees { get; set; } = new List<CalendarEventAttendee>();

    public virtual Company? Company { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Opportunity? Opportunity { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class CalendarEventAttendee
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid UserId { get; set; }

    public string AttendanceStatus { get; set; } = null!;

    public DateTime InvitedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public virtual CalendarEvent Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.Net;

namespace CRM.Api.Models;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public Guid? EntityId { get; set; }

    public string? OldData { get; set; }

    public string? NewData { get; set; }

    public IPAddress? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class Contact
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? CompanyId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? JobTitle { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company? Company { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    public virtual Workspace Workspace { get; set; } = null!;
}

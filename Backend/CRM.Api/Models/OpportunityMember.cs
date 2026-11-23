using System;
using System.Collections.Generic;

namespace CRM.Api.Models;

public partial class OpportunityMember
{
    public Guid Id { get; set; }

    public Guid OpportunityId { get; set; }

    public Guid UserId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Opportunity Opportunity { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

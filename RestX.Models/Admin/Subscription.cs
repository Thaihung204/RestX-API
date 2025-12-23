using RestX.Models.BaseModel;
using RestX.Models.Tenants;
using System;
using System.Collections.Generic;

namespace RestX.Models.Admin;

public partial class Subscription : Entity<Guid>
{
    public int TenantId { get; set; }

    public int PlanId { get; set; }

    public string? Status { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateOnly? RenewsAt { get; set; }

    public string? BillingCycle { get; set; }

    public virtual Plan Plan { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}

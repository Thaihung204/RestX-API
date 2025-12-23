using RestX.Models.BaseModel;
using RestX.Models.Tenants;
using System;
using System.Collections.Generic;

namespace RestX.Models.Admin;

public partial class Plan : Entity<int>
{

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? MaxUsers { get; set; }

    public int? MaxStorageMb { get; set; }

    public decimal? PriceMonthly { get; set; }

    public decimal? PriceYearly { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public virtual ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}

using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;

namespace RestX.Models.Tenants;

public partial class TenantSetting : Entity<Guid>
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string Key { get; set; } = null!;

    public string? Value { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}

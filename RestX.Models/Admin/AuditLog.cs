using RestX.Models.BaseModel;
using RestX.Models.Tenants;
using System;
using System.Collections.Generic;

namespace RestX.Models.Admin;

public partial class AuditLog : Entity<Guid>
{
    public int? TenantId { get; set; }

    public int? ActorAdminId { get; set; }

    public string Action { get; set; } = null!;

    public string? Description { get; set; }

    public string? Metadata { get; set; }

    public virtual Admin? ActorAdmin { get; set; }

    public virtual Tenant? Tenant { get; set; }
}

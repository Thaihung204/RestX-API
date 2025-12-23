using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using RestX.Models.Tenants;

namespace RestX.Models.Admin;

public partial class AuditLog : Entity<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid? ActorAdminId { get; set; }

    public string Action { get; set; } = null!;

    public string? Description { get; set; }

    public string? Metadata { get; set; }

    public virtual Admin? ActorAdmin { get; set; }

    public virtual Tenant? Tenant { get; set; }
}

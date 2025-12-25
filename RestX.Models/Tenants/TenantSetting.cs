using RestX.Models.BaseModel;

namespace RestX.Models.Tenants;

public partial class TenantSetting : Entity<Guid>
{
    public Guid TenantId { get; set; }

    public string Key { get; set; } = null!;

    public string? Value { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}

using RestX.Models.BaseModel;
using RestX.Models.Enum;

namespace RestX.Models.Tenants;

public class TenantRequest : Entity<Guid>
{
    public string Name { get; set; } = null!;
    public string Hostname { get; set; }

    public string? BusinessName { get; set; }
    public string? BusinessPrimaryPhone { get; set; }
    public string? BusinessEmailAddress { get; set; }

    public string? BusinessAddressLine1 { get; set; }
    public string? BusinessAddressLine2 { get; set; }
    public string? BusinessAddressLine3 { get; set; }
    public string? BusinessAddressLine4 { get; set; }
    public string? BusinessCountry { get; set; }

    public TenantRequestStatus tenantRequestStatus { get; set; } = TenantRequestStatus.Pending;
}
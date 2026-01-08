using RestX.Models.Admin;
using RestX.Models.BaseModel;

namespace RestX.Models.Tenants;

public class Tenant : Entity<Guid>
{
    public string Prefix { get; set; }

    public string Name { get; set; }

    public string LogoUrl { get; set; }

    public string FaviconUrl { get; set; }

    public string BackgroundUrl { get; set; }

    public string BaseColor { get; set; }

    public string PrimaryColor { get; set; }

    public string SecondaryColor { get; set; }

    public string NetworkIp { get; set; }

    public string ConnectionString { get; set; }

    public bool Status { get; set; }

    public string Domain { get; set; }

    public DateTime ExpiredAt { get; set; }

    public virtual ICollection<TenantSetting> TenantSettings { get; set; } = new List<TenantSetting>();
}

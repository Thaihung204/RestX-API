using RestX.Models.BaseModel;

namespace RestX.Models.Admin;

public partial class Plan : Entity<Guid>
{
    public string Name { get; set; }
    public string? Description { get; set; }

    public int? MaxUsers { get; set; }

    public int? MaxStorageMb { get; set; }

    public decimal? PriceMonthly { get; set; }

    public decimal? PriceYearly { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

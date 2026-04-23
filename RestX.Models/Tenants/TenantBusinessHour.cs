namespace RestX.Models.Tenants;

public class TenantBusinessHour
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public byte DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }

    public virtual Tenant Tenant { get; set; }
}

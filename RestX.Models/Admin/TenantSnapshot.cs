namespace RestX.Models.Admin;

public class TenantSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public string PeriodType { get; set; } = string.Empty;

    public decimal Revenue { get; set; }
    public decimal DiscountAmount { get; set; }

    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }

    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }

    public int NewReservations { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

namespace RestX.BLL.DataTranferObjects.Admin;

public class TenantSnapshotDto
{
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
}

public class TenantSnapshotDetailDto
{
    public Guid TenantId { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }

    public decimal Revenue { get; set; }
    public decimal DiscountAmount { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int NewReservations { get; set; }

    public List<SnapshotBreakdownItem> Breakdown { get; set; } = new();
}

public class SnapshotBreakdownItem
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal DiscountAmount { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int NewCustomers { get; set; }
    public int NewReservations { get; set; }
}

public class TenantSnapshotAggregateDto
{
    public DateOnly PeriodStart { get; set; }
    public string PeriodType { get; set; } = string.Empty;

    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int NewReservations { get; set; }

    public List<TenantSnapshotDto> Tenants { get; set; } = new();
}

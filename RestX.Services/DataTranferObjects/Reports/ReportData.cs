using RestX.BLL.DataTranferObjects.Dashboard;

namespace RestX.BLL.DataTranferObjects.Reports
{
    public class ReportData
    {
        public string ReportType { get; set; } = string.Empty;
        public string PeriodLabel { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; }

        public string TenantName { get; set; } = string.Empty;
        public string? TenantAddress { get; set; }
        public string? TenantPhone { get; set; }

        public DashboardSummary Summary { get; set; } = new();
        public RevenueTrend RevenueTrend { get; set; } = new();
        public OrderTrend OrderTrend { get; set; } = new();
        public TopDish TopDishes { get; set; } = new();
        public CustomerStats CustomerStats { get; set; } = new();
        public PromotionStats Promotions { get; set; } = new();
    }
}

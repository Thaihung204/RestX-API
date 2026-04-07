namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class DashboardOverview
    {
        public DashboardSummary Summary { get; set; }
        public RevenueTrend RevenueTrend { get; set; }
        public OrderTrend OrderTrend { get; set; }
        public TopDish TopDishes { get; set; }
        public TableStatus TableStatus { get; set; }
        public CustomerStats CustomerStats { get; set; }
    }
}

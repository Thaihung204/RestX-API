using RestX.BLL.DataTranferObjects.Dashboard;

namespace RestX.BLL.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardOverview> GetOverviewAsync(DashboardRequest request, int top = 5, string sortBy = "revenue");
        Task<DashboardSummary> GetSummaryAsync(DashboardRequest request);
        Task<RevenueTrend> GetRevenueTrendAsync(DashboardRequest request);
        Task<OrderTrend> GetOrderTrendAsync(DashboardRequest request);
        Task<TopDish> GetTopDishesAsync(DashboardRequest request, int top = 5, string sortBy = "revenue");
        Task<TableStatus> GetTableStatusAsync();
        Task<CustomerStats> GetCustomerStatsAsync(DashboardRequest request);
    }
}

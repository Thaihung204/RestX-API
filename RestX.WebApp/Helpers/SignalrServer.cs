using Microsoft.AspNetCore.SignalR;

namespace RestX.WebApp.Helpers
{
    public class SignalrServer : Hub
    {
        public const string OrderCreated = "orders.created";
        public const string OrderUpdated = "orders.updated";
        public const string OrderDeleted = "orders.deleted";

        public const string TableStatusChanged = "tables.status_changed";
        public const string TableLayoutUpdated = "tables.layout_updated";

        public const string DashboardOverviewUpdated = "dashboard.overview_updated";
        public const string DashboardSummaryUpdated = "dashboard.summary_updated";
        public const string DashboardTableStatusUpdated = "dashboard.table_status_updated";

        public async Task JoinTenantGroup(string tenantId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId.ToLower()}");

        public async Task LeaveTenantGroup(string tenantId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId.ToLower()}");
    }
}
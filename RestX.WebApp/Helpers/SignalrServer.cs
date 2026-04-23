using Microsoft.AspNetCore.SignalR;

namespace RestX.WebApp.Helpers
{
    public class SignalrServer : Hub
    {
        public const string OrderCreated = "orders.created";
        public const string OrderUpdated = "orders.updated";
        public const string OrderDeleted = "orders.deleted";

        public const string NotificationCreated = "notifications.created";
        public const string NotificationUpdated = "notifications.updated";
        public const string NotificationDeleted = "notifications.deleted";
        public const string NotificationPersonalCreated = "notifications.personal.created";

        public const string TableStatusChanged = "tables.status_changed";
        public const string TableLayoutUpdated = "tables.layout_updated";
        public const string TableSessionCreated = "tables.session_created";
        public const string TableSessionClosed = "tables.session_closed";

        public const string DashboardOverviewUpdated = "dashboard.overview_updated";
        public const string DashboardSummaryUpdated = "dashboard.summary_updated";
        public const string DashboardTableStatusUpdated = "dashboard.table_status_updated";

        public const string PaymentCompleted = "payments.completed";
        public const string PaymentCancelled = "payments.cancelled";

        public const string ReservationCreated = "reservations.created";
        public const string ReservationUpdated = "reservations.updated";
        public const string ReservationDeleted = "reservations.deleted";

        public async Task JoinTenantGroup(string tenantId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId.ToLower()}");

        public async Task LeaveTenantGroup(string tenantId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId.ToLower()}");

        public async Task JoinTenantUserGroup(string tenantId, string userId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId.ToLower()}:user_{userId}");

        public async Task LeaveTenantUserGroup(string tenantId, string userId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId.ToLower()}:user_{userId}");
    }
}
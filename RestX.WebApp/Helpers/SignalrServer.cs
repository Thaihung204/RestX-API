using Microsoft.AspNetCore.SignalR;

namespace RestX.WebApp.Helpers
{
    public class SignalrServer : Hub
    {
        public const string OrderCreated = "orders.created";
        public const string OrderUpdated = "orders.updated";
        public const string OrderDeleted = "orders.deleted";

        public async Task JoinTenantGroup(string tenantId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");

        public async Task LeaveTenantGroup(string tenantId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }
}
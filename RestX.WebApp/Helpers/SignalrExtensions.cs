using Microsoft.AspNetCore.SignalR;

namespace RestX.WebApp.Helpers
{
    public static class SignalrExtensions
    {
        public static Task BroadcastToTenant(
            this IHubContext<SignalrServer> hub, Guid tenantId, string eventName, object payload)
            => hub.Clients.Group($"tenant_{tenantId}").SendAsync(eventName, payload);

        public static Task BroadcastToTenantUser(
            this IHubContext<SignalrServer> hub, Guid tenantId, string userId, string eventName, object payload)
            => hub.Clients.Group($"tenant_{tenantId.ToString().ToLower()}:user_{userId}")
                .SendAsync(eventName, payload);
    }
}

using Microsoft.AspNetCore.SignalR;

namespace RestX.WebApp.Helpers
{
    public static class SignalrExtensions
    {
        public static Task BroadcastToTenant(
            this IHubContext<SignalrServer> hub,Guid tenantId,string eventName,object payload)
            => hub.Clients.Group($"tenant_{tenantId.ToString().ToLower()}").SendAsync(eventName, payload);
    }
}

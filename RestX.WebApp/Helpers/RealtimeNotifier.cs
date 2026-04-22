using Microsoft.AspNetCore.SignalR;
using RestX.BLL.Interfaces;

namespace RestX.WebApp.Helpers
{
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<SignalrServer> hub;

        public RealtimeNotifier(IHubContext<SignalrServer> hub)
        {
            this.hub = hub;
        }

        public Task PaymentCompletedAsync(Guid tenantId, Guid paymentId, Guid? orderId)
            => hub.BroadcastToTenant(tenantId, SignalrServer.PaymentCompleted, new { paymentId, orderId });

        public Task PaymentCancelledAsync(Guid tenantId, Guid? paymentId)
            => hub.BroadcastToTenant(tenantId, SignalrServer.PaymentCancelled, new { paymentId });

        public Task OrderUpdatedAsync(Guid tenantId, Guid orderId)
            => hub.BroadcastToTenant(tenantId, SignalrServer.OrderUpdated, new { id = orderId });

        public Task TableSessionClosedAsync(Guid tenantId, Guid tableId)
            => hub.BroadcastToTenant(tenantId, SignalrServer.TableSessionClosed, new { tableId });

        public Task ReservationUpdatedAsync(Guid tenantId, Guid reservationId)
            => hub.BroadcastToTenant(tenantId, SignalrServer.ReservationUpdated, new { id = reservationId });
    }
}

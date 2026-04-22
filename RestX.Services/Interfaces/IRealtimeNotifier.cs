namespace RestX.BLL.Interfaces
{
    public interface IRealtimeNotifier
    {
        Task PaymentCompletedAsync(Guid tenantId, Guid paymentId, Guid? orderId);
        Task PaymentCancelledAsync(Guid tenantId, Guid? paymentId);
        Task OrderUpdatedAsync(Guid tenantId, Guid orderId);
        Task TableSessionClosedAsync(Guid tenantId, Guid tableId);
        Task ReservationUpdatedAsync(Guid tenantId, Guid reservationId);
    }
}

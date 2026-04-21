namespace RestX.BLL.Interfaces
{
    public interface IRealtimeNotifier
    {
        Task OrderUpdatedAsync(Guid tenantId, Guid orderId);
        Task TableSessionClosedAsync(Guid tenantId, Guid tableId);
        Task ReservationUpdatedAsync(Guid tenantId, Guid reservationId);
    }
}

using RestX.BLL.DataTranferObjects.Orders;

namespace RestX.BLL.Interfaces
{
    public interface IOrderService
    {
        Task<OrderSearchResult> GetAllOrders(OrderSearch model);
        Task<Order?> GetOrderById(Guid id);
        Task<Order> CheckSessionBeforeOrder(Order order, string userId);
        Task<Models.Orders.Order> UpsertOrder(Order order, string userId);
        Task<Guid> UpdateOrder(Guid id, Order order, string userId);
        Task DeleteOrder(Guid id);
        Task<bool> UpdateStatus(Guid orderId, int statusId, string userId);
        Task<bool> UpdateOrderDetailStatus(Guid orderDetailId, int statusId, string userId);
        Task<IEnumerable<OrderDetail>> GetAllOrderDetails();
        Task<Order?> GetOrderByTableId(Guid tableId);

        Task<byte[]> ExportAsync(OrderSearch filter);
        Task<ApplyDiscountResponse> ApplyDiscount(Guid orderId, ApplyDiscountRequest request);
        Task RemoveDiscount(Guid orderId);
    }
}

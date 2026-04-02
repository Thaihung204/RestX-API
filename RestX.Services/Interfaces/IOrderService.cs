using RestX.BLL.DataTranferObjects.Orders;
using RestX.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Interfaces
{
    public interface IOrderService
    {
        Task<OrderSearchResult> GetAllOrders(OrderSearch model);
        Task<Order?> GetOrderById(Guid id);
        Task<Guid> CreateOrder(Order order, string userId);
        Task<Guid> UpdateOrder(Guid id, Order order, string userId);
        Task DeleteOrder(Guid id);
        Task<bool> UpdateStatus(Guid orderId, int statusId, string userId);
        Task<bool> UpdateOrderDetailStatus(Guid orderDetailId, int statusId, string userId);
        Task<byte[]> ExportAsync(OrderSearch filter);
    }
}

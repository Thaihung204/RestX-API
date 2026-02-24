using RestX.BLL.DataTranferObjects.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderItem>> GetAllOrders();
        Task<OrderItem?> GetOrderById(Guid id);
        Task<Guid> UpsertOrder(OrderItem order);
        Task DeleteOrder(Guid id);
        Task<OrderItem> CreateOrder(OrderItem orderItem);
    }
}

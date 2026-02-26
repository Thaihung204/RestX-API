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
        Task<IEnumerable<Order>> GetAllOrders();
        Task<Order?> GetOrderById(Guid id);
        Task<Guid> CreateOrder(Order order, string userId);
        Task<Guid> UpdateOrder(Guid id, Order order, string userId);
        Task DeleteOrder(Guid id);
    }
}

using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.DataTranferObjects.Reservation;

namespace RestX.BLL.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportCustomersAsync(CustomerFilterParams filter);
        Task<byte[]> ExportReservationsAsync(ReservationFilterParams filter);
        Task<byte[]> ExportOrdersAsync(OrderSearch filter);
    }
}

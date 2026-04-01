using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.DataTranferObjects.Reservation;

namespace RestX.BLL.Interfaces
{
    public interface ICsvExportService
    {
        Task<byte[]> ExportCustomersCsvAsync(CustomerFilterParams filter);
        Task<byte[]> ExportReservationsCsvAsync(ReservationFilterParams filter);
        Task<byte[]> ExportOrdersCsvAsync(OrderSearch filter);
    }
}

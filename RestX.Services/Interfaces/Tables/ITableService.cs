using RestX.BLL.DataTranferObjects.Table;
using RestX.Models.Enum;
using RestX.Models.Reservations;

namespace RestX.BLL.Interfaces.Tables
{
    public interface ITableService
    {
        Task<IEnumerable<TableItem>> GetAllTables();
        Task<TableItem?> GetTableById(Guid id);
        Task<TableItem> UpsertTable(Guid? id, TableItem request);
        Task DeleteTable(Guid id);
        Task<TableItem> ChangeTableStatus(Guid id, TableStatus status);
        Task<TableSession> CreateTableSession(Guid tableId, string userId, Guid? customerId = null, Guid? reservationId = null);
        Task<TableSession?> GetActiveTableSession(Guid tableId);
        Task<int> CloseTableSession(Guid orderId);
    }
}

using RestX.BLL.DataTranferObjects.Table;

namespace RestX.BLL.Interfaces.Tables
{
    public interface ITableService
    {
        Task<IEnumerable<TableItem>> GetAllTables();
        Task<TableItem?> GetTableById(Guid id);
        Task<TableItem> UpsertTable(Guid? id, TableRequest request);
        Task DeleteTable(Guid id);
    }
}

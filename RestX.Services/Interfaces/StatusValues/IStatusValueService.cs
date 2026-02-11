using RestX.BLL.DataTranferObjects.StatusValue;

namespace RestX.BLL.Interfaces.StatusValues
{
    public interface IStatusValueService
    {
        Task<IEnumerable<StatusValueItem>> GetByType(string typeCode);
        Task<StatusValueItem?> GetById(int id);
        Task<StatusValueItem> Upsert(string typeCode, int? id, UpsertStatusValueRequest request);
        Task Delete(int id);
    }
}

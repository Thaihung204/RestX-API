using RestX.BLL.DataTranferObjects.Status;

namespace RestX.BLL.Interfaces.Status
{
    public interface IStatusValueService
    {
        Task<IEnumerable<StatusValues>> GetStatusByType(string typeCode);
        Task<StatusValues?> GetStatusValueById(int id);
        Task<StatusValues> UpsertStatusValue(string typeCode, int? id, StatusValues request);
        Task DeleteStatusValue(string typeCode, int id);
    }
}

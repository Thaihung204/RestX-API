using RestX.BLL.DataTranferObjects.Status;

namespace RestX.BLL.Interfaces.Status
{
    public interface IStatusValueService
    {
        Task<IEnumerable<StatusValues>> GetStatusByType(string typeCode);
        Task<StatusValues?> GetStatusById(int id);
        Task<StatusValues> UpsertStatus(string typeCode, int? id, StatusValues request);
        Task DeleteStatus(string typeCode, int id);
    }
}

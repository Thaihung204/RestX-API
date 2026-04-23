using RestX.BLL.DataTranferObjects.Status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Interfaces.Status
{
    public interface IStatusValueService
    {
        Task<IEnumerable<StatusValues>> GetStatuses(string typeCode);
        Task<StatusValues?> GetStatusValueById(int id);
        Task<StatusValues> UpsertStatusValue(string typeCode, int? id, StatusValues request);
        Task DeleteStatusValue(string typeCode, int id);
        Task UpdateDisplayOrder(string typeCode, List<StatusValues> items);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace RestX.BLL.Interfaces
{
    using Hangfire.Server;
    using RestX.BLL.DataTranferObjects.Share;
    using RestX.Models.Triggers;
    using System.Threading.Tasks;

    public interface ITriggerService
    {
        Task CheckForTriggers(Guid tenantId, List<TriggerCheckData> items);
        Task<List<DataTransferObjects.Triggers.TriggerObject>> GetTriggerObjects();
        Task<List<DataTransferObjects.Triggers.TriggerObjectProperties>> GetTriggerObjectProperties(int objectId);
        List<SelectOption> GetTriggerTypes();
        List<SelectOption> GetTriggerCriteriaTypes();
        Task<List<SelectOption>> GetTriggerActionTypes();
        Task<List<DataTransferObjects.Triggers.Trigger>> GetTriggers();
        Task<DataTransferObjects.Triggers.Trigger> GetTriggerById(Guid triggerId);
        Task UpsertTrigger(DataTransferObjects.Triggers.Trigger data, string userId);
        Task DeleteTrigger(Guid triggerId, string userId);
        Task SetTriggerStatus(Guid triggerId, bool isActive, string userId);
    }
}

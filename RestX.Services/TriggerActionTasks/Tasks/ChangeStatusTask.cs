using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;
using RestX.Models.Triggers;

namespace RestX.BLL.TriggerActionTasks.Tasks
{
    public class ChangeStatusTask: BaseTask
    {
        public ChangeStatusTask( IRepository repo, ActiveTenant tenant) : base(repo, tenant)
        {
        }

        public override async Task ProcessTask(TriggerCheckData item, TriggerAction triggerAction, bool requiresTemplateData, int? scheduledActionId = null)
        {
            
        }
    }
}

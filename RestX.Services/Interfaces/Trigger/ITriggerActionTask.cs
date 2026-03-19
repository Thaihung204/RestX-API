using System;
using System.Collections.Generic;
using System.Text;

namespace RestX.BLL.Interfaces
{
    using RestX.Models.Triggers;
    using System.Threading.Tasks;

    public interface ITriggerActionTask
    {
        Task ProcessTask(TriggerCheckData item, TriggerAction triggerAction, bool requiresTemplateData, int? scheduledActionId = null);
    }
}

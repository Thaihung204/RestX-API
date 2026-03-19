using RestX.BLL.Interfaces;
using RestX.Models.Tenants;
using RestX.Models.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.TriggerActionTasks.Tasks
{
    public class BaseTask : ITriggerActionTask
    {
        public ActiveTenant CurrentTenant;
        public IRepository TenantRepo;
        public dynamic Data;

        public BaseTask(IRepository repo, ActiveTenant tenant)
        {
            this.TenantRepo = repo;
            this.CurrentTenant = tenant;
        }

        public virtual async Task ProcessTask(TriggerCheckData item, TriggerAction triggerAction, bool requiresTemplateData, int? scheduledActionId = null)
        {
            switch (item.ObjectName)
            {
                case "Enquiry":
                    
                    break;
                
                default:
                    object id = null;
                    if (int.TryParse(item.CurrentValues["Id"].ToString(), out var objectId))
                    {
                        id = objectId;
                    }
                    else if (Guid.TryParse(item.CurrentValues["Id"].ToString(), out var objectGuid))
                    {
                        id = objectGuid;
                    }

                    break;
            }

            if (requiresTemplateData)
            {
                switch (item.ObjectName)
                {
                    case "Enquiry":
                        Data = new
                        {
                            //Enquiry = await this.TemplateService.GetEnquiryDataForPlaceHolders(this.EnquiryId),
                            //System = this.TemplateService.GetSystemDataForPlaceholders(this.CurrentTenant)
                        };
                        break;
                    
                }
            }
        }
    }
}

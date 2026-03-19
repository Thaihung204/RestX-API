using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class ScheduledActionsResult
    {
        public List<ScheduledAction> ScheduledActions { get; set; } = new List<ScheduledAction>();
        public List<ScheduledActionHistory> ScheduledActionHistory { get; set; } = new List<ScheduledActionHistory>();
    }
}

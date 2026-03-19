using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class ScheduledActionSkipRequest
    {
        public bool Skip { get; set; }
        public string Reason { get; set; }
    }
}

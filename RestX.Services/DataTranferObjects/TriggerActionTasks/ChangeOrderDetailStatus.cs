using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.TriggerActionTasks
{
    public class ChangeOrderDetailStatus
    {
        public int Type { get; set; }
        public string Subject { get; set; }
        public string Text { get; set; }
    }
}

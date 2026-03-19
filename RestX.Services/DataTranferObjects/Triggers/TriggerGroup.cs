using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestX.Models.Triggers;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class TriggerGroup
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public TriggerLogicType LogicType { get; set; }
    }
}

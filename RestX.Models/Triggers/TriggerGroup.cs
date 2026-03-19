using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Triggers
{
    /// <summary>
    /// Allows for criteria to be grouped together
    /// </summary>
    public class TriggerGroup : Entity<int>
    {
        public string Name { get; set; }
        public TriggerLogicType LogicType { get; set; }
        public virtual List<TriggerCriteria> Criteria { get; set; }
    }
}

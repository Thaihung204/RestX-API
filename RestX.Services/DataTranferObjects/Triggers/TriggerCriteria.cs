using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using RestX.Models.Triggers;

namespace RestX.BLL.DataTransferObjects.Triggers

{
    public class TriggerCriteria
    {
        public int? Id { get; set; }
        public int? TriggerCriteriaGroupId { get; set; }
        public TriggerLogicType LogicType { get; set; }
        public TriggerCriteriaType Type { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public TriggerGroup Group { get; set; }
        public string ComputedDescription { get;  set; }
    }
}

using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RestX.Models.Triggers
{
    /// <summary>
    /// Criteria for a trigger that must be met for the trigger to fire
    /// </summary>
    public class TriggerCriteria : Entity<int>
    {
        public Guid TriggerId { get; set; }
        public int? TriggerCriteriaGroupId { get; set; }
        public TriggerCriteriaType Type { get; set; }
        public TriggerLogicType LogicType { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public string ComputedDescription { get; set; }
        [ForeignKey("TriggerId")]
        public virtual Trigger Trigger { get; set; }
        [ForeignKey("TriggerCriteriaGroupId")]
        public virtual TriggerGroup Group { get; set; }
    }
}

using RestX.Models.Triggers;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class TriggerActionScheduledCriteria
    {
        public int? Id { get; set; }
        public int TriggerActionId { get; set; }
        public int? TriggerCriteriaGroupId { get; set; }
        public TriggerCriteriaType? CriteriaType { get; set; }
        public TriggerLogicType? LogicType { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public string ComputedDescription { get; set; }
        public TriggerGroup Group { get; set; }
    }
}

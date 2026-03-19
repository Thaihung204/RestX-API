using System.Collections.Generic;
using System.Dynamic;
using RestX.Models.Triggers;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class TriggerAction
    {
        public int? Id { get; set; }
        public TriggerActionType Type { get; set; }
        public string Action { get; set; }
        public ExpandoObject CustomProperties { get; set; }
        public bool Scheduled { get; set; }
        public int? ScheduledTimeOffsetDays { get; set; }
        public int? ScheduledTimeOffsetHours { get; set; }
        public int? ScheduledTimeOffsetMinutes { get; set; }
        public int? ScheduledTimeOffsetTimeHours { get; set; }
        public int? ScheduledTimeOffsetTimeMinutes { get; set; }
        public List<TriggerGroup> Groups { get; set; } = new List<TriggerGroup>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RestX.Models;
using RestX.Models.Triggers;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class Trigger
    {
        public Guid? Id { get; set; }
        public TriggerType Type { get; set; }
        public int TriggerObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TriggeredByRole { get; set; }
        public bool? IsActive { get; set; } = true;
        public List<TriggerAction> Actions { get; set; } = new List<TriggerAction>();
        public List<TriggerCriteria> Criteria { get; set; } = new List<TriggerCriteria>();
        public List<TriggerGroup> Groups { get; set; } = new List<TriggerGroup>();
    }
}

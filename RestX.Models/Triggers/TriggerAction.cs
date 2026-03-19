using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Newtonsoft.Json;
using RestX.Models.BaseModel;

namespace RestX.Models.Triggers
{
    /// <summary>
    /// This is action to take when a trigger is fired and the trigger criteria are met
    /// </summary>
    public class TriggerAction : Entity<int>
    {
        /// <summary>
        /// The Id of the trigger
        /// </summary>
        public Guid TriggerId { get; set; }

        /// <summary>
        /// The type of action
        /// </summary>
        public TriggerActionType Type { get; set; }

        /// <summary>
        /// The name of the action
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// The trigger this action is associated with
        /// </summary>
        [ForeignKey("TriggerId")]
        [JsonIgnore]
        public virtual Trigger Trigger { get; set; }
    }
}

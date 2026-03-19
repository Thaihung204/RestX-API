using Newtonsoft.Json;
using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RestX.Models.Triggers
{
    /// <summary>
    /// A trigger contains a list of criteria to meet and a list of actions to take if the criteria is met.
    /// </summary>
    public class Trigger : Entity<Guid>
    {
        /// <summary>
        /// The type of trigger, currently supports insert, update and delete
        /// </summary>
        public TriggerType Type { get; set; }
        /// <summary>
        /// The object this relates to. Objects relate to Entities but are stored in the database.
        /// </summary>
        public int TriggerObjectId { get; set; }
        /// <summary>
        /// A name for the trigger
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// A description of the trigger
        /// </summary>
        public string Description { get; set; }

        public bool? IsActive { get; set; } = true;
        /// <summary>
        /// The triggering object
        /// </summary>
        [ForeignKey("TriggerObjectId")]
        public virtual TriggerObject Object { get; set; }

        /// <summary>
        /// The associated actions the trigger should take when the trigger is fired
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<TriggerAction> Actions { get; set; } = new List<TriggerAction>();

        /// <summary>
        /// The criteria that must be met when the trigger is fired
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<TriggerCriteria> Criteria { get; set; } = new List<TriggerCriteria>();
    }
}

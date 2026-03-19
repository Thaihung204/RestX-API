using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace RestX.Models.Triggers
{
    public enum TriggerCriteriaType
    {
        [Description("Any property change")]
        AnyPropertyChange,
        [Description("Is updated with")]
        SpecificPropertyNewValue,
        [Description("Did Have the value")]
        SpecificPropertyOldValue,
        [Description("Has the value")]
        SpecificPropertyValue,
        [Description("Does not have the value")]
        SpecificPropertyValueNotEquals,
        [Description("Is greater than")]
        IsGreaterThan,
        [Description("Is less than")]
        IsLessThan,
        [Description("Is updated")]
        IsUpdated,
        [Description("Contains")]
        Contains
    }
}

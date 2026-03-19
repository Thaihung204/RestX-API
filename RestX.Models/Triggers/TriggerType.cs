using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace RestX.Models.Triggers
{
    public enum TriggerType
    {
        [Description("On Creation")]
        Insert,
        [Description("On Update")]
        Update,
        [Description("On Delete")]
        Delete
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class TriggerObjectGroupedProperties
    {
        public string Group { get; set; }
        public List<TriggerObjectProperties> Properties { get; set; }
    }
}

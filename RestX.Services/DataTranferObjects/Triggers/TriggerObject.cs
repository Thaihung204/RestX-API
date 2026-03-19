using System;
using System.Collections.Generic;
using System.Text;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class TriggerObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ObjectName { get; set; }
        public List<TriggerObjectProperties> Properties { get; set; }
    }
}

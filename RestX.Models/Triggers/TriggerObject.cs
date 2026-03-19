using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestX.Models.Triggers
{
    public class TriggerObject : Entity<int>
    {
        public string Name { get; set; }
        public string ObjectName { get; set; }
        public string FullAssemblyName { get; set; }
    }
}

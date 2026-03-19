using System.Collections.Generic;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class TriggerObjectProperties
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ValueType { get; set; }
        public string Value { get; set; }
        public List<TriggerObjectProperties> ChildProperties { get; set; }
        public string LookupUrl { get; set; }
        public bool CanBeUpdated { get; set; }
    }
}

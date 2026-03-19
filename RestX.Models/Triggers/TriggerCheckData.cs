using RestX.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Triggers
{
    /// <summary>
    /// This class is used to hold the data relevant to a datbase change which is then used by the trigger service to check if the changes should trigger any triggers
    /// </summary>
    public class TriggerCheckData
    {
        public dynamic ObjectId { get; set; }
        public string ObjectName { get; set; }
        public TriggerCheckType Type { get; set; }
        public Dictionary<string, string> OriginalValues { get; set; }
        public Dictionary<string, string> CurrentValues { get; set; }
    }
}

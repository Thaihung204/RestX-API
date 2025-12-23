using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.BaseModel
{
    internal class TriggerPropertyAttribute : Attribute
    {
        public string DisplayName { get; set; }
    }
}

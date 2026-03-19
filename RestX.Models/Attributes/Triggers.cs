using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Attributes
{
    public class UsedByTriggers : Attribute
    {
    }
    public class TriggerProperty : Attribute
    {
        private string lookupUrl;
        private string displayName;
        private bool canBeUpdated = true;

        public TriggerProperty()
        {
        }

        public virtual string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }
        public virtual string LookupUrl
        {
            get { return lookupUrl; }
            set { lookupUrl = value; }
        }

        public virtual bool CanBeUpdated
        {
            get { return canBeUpdated; }
            set { canBeUpdated = value; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class ScheduledAction
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public DateTime ScheduledDateTimeUtc { get; set; }
        public DateTime ScheduledDateTimeLocal { get; set; }
        public bool SkipAction { get; set; }
        public string SkippedBy { get; set; }
        public DateTime? SkippedDate { get; set; }
        public string SkippedMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Quote { get; set; }
        public string Booking { get; set; }
    }
}

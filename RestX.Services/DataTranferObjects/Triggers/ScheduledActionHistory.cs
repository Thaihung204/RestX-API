using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTransferObjects.Triggers
{
    public class ScheduledActionHistory
    {

        public string Action { get; set; }
        public DateTime ScheduledDateTimeUtc { get; set; }
        public DateTime ScheduledDateTimeLocal { get; set; }
        public bool WasSkipped { get; set; }
        public string SkippedBy { get; set; }
        public DateTime? SkippedDate { get; set; }
        public string SkippedMessage { get; set; }
        public DateTime DateProcessedUtc { get; set; }
        public DateTime DateProcessedLocal { get; set; }
        public bool DidSucceed { get; set; }
        public string ErrorMessage { get; set; }
        public string Quote { get; set; }
        public string Booking { get; set; }
    }
}

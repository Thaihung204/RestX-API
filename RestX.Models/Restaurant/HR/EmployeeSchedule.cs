using RestX.Models.BaseModel;
using RestX.Models.Restaurant.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.HR
{
    public partial class EmployeeSchedule : Entity<Guid>
    {
        public Guid EmployeeId { get; set; }

        [Column(TypeName = "date")]
        public DateTime WorkDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public Guid StatusId { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public virtual Employee Employee { get; set; } = null!;
        public virtual StatusValue Status { get; set; }
    }
}

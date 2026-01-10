using RestX.Models.BaseModel;
using RestX.Models.Orders;
using RestX.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Reservations
{
    public partial class TableSession : Entity<Guid>
    {
        public Guid TableId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? CurrentOrderId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Table Table { get; set; } = null!;
        public virtual Reservation? Reservation { get; set; }
        public virtual Order? CurrentOrder { get; set; }
    }
}

using RestX.Models.BaseModel;
using RestX.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Reservations
{
    public partial class ReservationTable : Entity<Guid>
    {
        public Guid ReservationId { get; set; }
        public Guid TableId { get; set; }

        public virtual Reservation Reservation { get; set; } = null!;
        public virtual Table Table { get; set; } = null!;
    }
}

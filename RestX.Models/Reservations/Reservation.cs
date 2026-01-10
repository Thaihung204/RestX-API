using RestX.Models.BaseModel;
using RestX.Models.Common;
using RestX.Models.Customers;
using RestX.Models.Orders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Reservations
{
    public partial class Reservation : Entity<Guid>
    {
        public Guid CustomerId { get; set; }

        [Range(1, 100)]
        public int NumberOfGuests { get; set; }

        public DateTime Time { get; set; }

        [MaxLength(1000)]
        public string? SpecialRequests { get; set; }

        public Guid ReservationStatusId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal DepositAmount { get; set; } = 0;

        public bool DepositPaid { get; set; } = false;

        public DateTime? CheckedInAt { get; set; }

       
        public virtual Customer Customer { get; set; } = null!;
        public virtual StatusValue ReservationStatus { get; set; }
        public virtual ICollection<ReservationTable> ReservationTables { get; set; } = new HashSet<ReservationTable>();
        public virtual ICollection<TableSession> TableSessions { get; set; } = new HashSet<TableSession>();
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
        public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
    }
}

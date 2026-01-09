using RestX.Models.BaseModel;
using RestX.Models.Restaurant.Feedbacks;
using RestX.Models.Restaurant.Identity;
using RestX.Models.Restaurant.Loyalty;
using RestX.Models.Restaurant.Orders;
using RestX.Models.Restaurant.Reservations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Customers
{
    public partial class Customer : Entity<Guid>
    {
        public Guid UserId { get; set; }

        [MaxLength(20)]
        public string MembershipLevel { get; set; } = "BRONZE";

        [Range(0, int.MaxValue)]
        public int LoyaltyPoints { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public virtual User User { get; set; } = null!;
        public virtual ICollection<Reservation> Reservations { get; set; } = new HashSet<Reservation>();
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
        public virtual ICollection<PointsTransaction> PointsTransactions { get; set; } = new HashSet<PointsTransaction>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new HashSet<Feedback>();
    }
}

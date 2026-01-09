using RestX.Models.BaseModel;
using RestX.Models.Restaurant.Common;
using RestX.Models.Restaurant.Customers;
using RestX.Models.Restaurant.Feedbacks;
using RestX.Models.Restaurant.HR;
using RestX.Models.Restaurant.Loyalty;
using RestX.Models.Restaurant.Orders;
using RestX.Models.Restaurant.Promotions;
using RestX.Models.Restaurant.Reservations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Orders
{
    public partial class Order : Entity<Guid>
    {
        [Required]
        [MaxLength(20)]
        public string Reference { get; set; } = string.Empty;

        public Guid? CustomerId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid OrderStatusId { get; set; }
        public Guid PaymentStatusId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal SubTotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal ServiceCharge { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal TotalAmount { get; set; } = 0;

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public Guid? HandledBy { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual Reservation? Reservation { get; set; }
        public virtual StatusValue OrderStatus { get; set; }
        public virtual StatusValue PaymentStatus { get; set; }
        public virtual Employee? Handler { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new HashSet<OrderDetail>();
        public virtual ICollection<OrderTable> OrderTables { get; set; } = new HashSet<OrderTable>();
        public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
        public virtual ICollection<PromotionHistory> PromotionHistories { get; set; } = new HashSet<PromotionHistory>();
        public virtual ICollection<PointsTransaction> PointsTransactions { get; set; } = new HashSet<PointsTransaction>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new HashSet<Feedback>();
    }
}

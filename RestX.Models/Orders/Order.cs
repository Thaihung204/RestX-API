using RestX.Models.Attributes;
using RestX.Models.BaseModel;
using RestX.Models.Common;
using RestX.Models.Customers;
using RestX.Models.Enum;
using RestX.Models.Feedbacks;
using RestX.Models.HR;
using RestX.Models.Loyalty;
using RestX.Models.Promotions;
using RestX.Models.Reservations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestX.Models.Orders
{
    public partial class Order : Entity<Guid>
    {
        [Required]
        [MaxLength(20)]
        public string Reference { get; set; } = string.Empty;

        public Guid? CustomerId { get; set; }
        public Guid? ReservationId { get; set; }
        [TriggerProperty(DisplayName = "Status")]
        public int OrderStatusId { get; set; }

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
        public decimal TotalAmount { get; private set; } = 0;

        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public Guid? HandledBy { get; set; }

        [NotMapped]
        public bool IsPaid { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual Reservation? Reservation { get; set; }
        public virtual Employee? Handler { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new HashSet<OrderDetail>();
        public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
        public virtual ICollection<PromotionHistory> PromotionHistories { get; set; } = new HashSet<PromotionHistory>();
        public virtual ICollection<PointsTransaction> PointsTransactions { get; set; } = new HashSet<PointsTransaction>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new HashSet<Feedback>();
        public virtual ICollection<TableSession> TableSessions { get; set; } = new HashSet<TableSession>();

        public void CalculateTotalAmount()
        {
            TotalAmount = (SubTotal - DiscountAmount) + TaxAmount + ServiceCharge;
        }
    }
}
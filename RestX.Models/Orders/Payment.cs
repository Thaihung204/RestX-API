using RestX.Models.BaseModel;
using RestX.Models.Enum;
using RestX.Models.HR;
using RestX.Models.Reservations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestX.Models.Orders
{
    public partial class Payment : Entity<Guid>
    {
        public Guid? OrderId { get; set; }
        public Guid? ReservationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethodId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? TransactionId { get; set; }

        public long? PayOSOrderCode { get; set; }

        [MaxLength(500)]
        public string? CheckoutUrl { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal Amount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal CashReceive { get; set; } = 0;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow.AddHours(7);

        public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

        public PaymentPurpose Purpose { get; set; } = PaymentPurpose.Order;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal Cashback { get; set; } = 0;

        public DateTime? RefundDate { get; set; }

        public Guid? ProcessedBy { get; set; }

        public virtual Order? Order { get; set; }
        public virtual Reservation? Reservation { get; set; }
        public virtual Employee? Processor { get; set; }
    }
}

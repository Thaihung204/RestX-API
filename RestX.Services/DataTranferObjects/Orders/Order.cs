using RestX.BLL.DataTranferObjects.Table;
using RestX.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Orders
{
    public class Order
    {
        public Guid? Id { get; set; }

        [MaxLength(20)]
        public string? Reference { get; set; }

        [Required]
        public Guid TableId { get; set; }

        public Guid CustomerId { get; set; }
        public Guid? ReservationId { get; set; }

        public OrderStatus OrderStatusId { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string PaymentStatusName => PaymentStatus.ToString();

        [Range(0, 999999999.99)]
        public decimal? SubTotal { get; set; }

        [Range(0, 999999999.99)]
        public decimal? DiscountAmount { get; set; }

        [Range(0, 999999999.99)]
        public decimal? TaxAmount { get; set; }

        [Range(0, 999999999.99)]
        public decimal? ServiceCharge { get; set; }

        [Range(0, 999999999.99)]
        public decimal TotalAmount { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public Guid? HandledBy { get; set; }

        public List<TableSessionInfo>? tableSessions{ get; set; } = new();
        public List<OrderDetail> OrderDetails { get; set; }
    }
}
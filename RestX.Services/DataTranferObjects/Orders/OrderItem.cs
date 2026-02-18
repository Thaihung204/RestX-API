using RestX.Models.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Orders
{
    public class OrderItem
    {
        public Guid? Id { get; set; }

        [MaxLength(20)]
        public string? Reference { get; set; }

        public Guid? CustomerId { get; set; }
        public Guid? ReservationId { get; set; }

        public OrderStatus OrderStatusId { get; set; } = OrderStatus.Reserved;
        public int PaymentStatusId { get; set; }

        [Range(0, 999999999.99)]
        public decimal SubTotal { get; set; }

        [Range(0, 999999999.99)]
        public decimal DiscountAmount { get; set; }

        [Range(0, 999999999.99)]
        public decimal TaxAmount { get; set; }

        [Range(0, 999999999.99)]
        public decimal ServiceCharge { get; set; }

        [Range(0, 999999999.99)]
        public decimal TotalAmount { get; set; }

        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public Guid? HandledBy { get; set; }

        public List<Guid> TableIds { get; set; } = new();

        public List<OrderDetailItem> Details { get; set; } = new();
    }
}

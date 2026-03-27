using RestX.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Reservation
{
    public class DepositStatusResponse
    {
        public Guid ReservationId { get; set; }
        public decimal DepositAmount { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public bool IsPaid { get; set; }
        public string? CheckoutUrl { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
    }

    public class RefundCalculationResponse
    {
        public decimal DepositAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public int RefundPercentage { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class RefundRequest
    {
        [Required]
        public RefundInitiator InitiatedBy { get; set; }
    }
}

using RestX.Models.Enum;

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
        public string? PaymentStatusName => PaymentStatus?.ToString();
        public string? ReservationStatus { get; set; }
        public Guid? OrderId { get; set; }
    }
}

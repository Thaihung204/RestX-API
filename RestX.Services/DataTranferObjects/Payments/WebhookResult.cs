namespace RestX.BLL.DataTranferObjects.Payments
{
    public class WebhookResult
    {
        public bool Success { get; set; }
        public Guid PaymentId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? ReservationId { get; set; }
        public decimal Amount { get; set; }
        public bool IsDeposit { get; set; }
    }
}

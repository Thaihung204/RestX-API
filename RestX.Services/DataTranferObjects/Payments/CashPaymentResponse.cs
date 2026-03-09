namespace RestX.BLL.DataTranferObjects.Payments
{
    public class CashPaymentResponse
    {
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public decimal CashReceive { get; set; }
        public decimal Cashback { get; set; }
    }
}

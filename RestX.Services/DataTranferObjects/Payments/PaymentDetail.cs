namespace RestX.BLL.DataTranferObjects.Payments
{
    public class PaymentDetail
    {
        public Guid Id { get; set; }
        public Guid? OrderId { get; set; }
        public string PaymentMethodId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long? PayOSOrderCode { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? TransactionId { get; set; }
        public decimal CashReceive { get; set; }
        public decimal Cashback { get; set; }
        public DateTime PaymentDate { get; set; }
        public int PaymentStatusId { get; set; }
        public string? PaymentStatusName { get; set; }
        public string? PaymentStatusCode { get; set; }
    }
}

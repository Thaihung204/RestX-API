namespace RestX.BLL.DataTranferObjects.Payments
{
    public class CreatePaymentLinkResponse
    {
        public Guid PaymentId { get; set; }
        public long OrderCode { get; set; }
        public string CheckoutUrl { get; set; } = string.Empty;
        public bool ZeroAmount { get; set; } = false;
    }
}

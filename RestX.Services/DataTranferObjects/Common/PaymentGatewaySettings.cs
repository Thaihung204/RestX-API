namespace RestX.BLL.DataTranferObjects.Common
{
    public class PaymentGatewaySettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string ReturnDepositUrl { get; set; } = string.Empty;
        public string CancelDepositUrl { get; set; } = string.Empty;
    }
}

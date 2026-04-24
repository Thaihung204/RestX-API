using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Payments
{
    public class CashPaymentRequest
    {
        [Required]
        [Range(0, 999999999.99)]
        public decimal CashReceive { get; set; }
    }
}

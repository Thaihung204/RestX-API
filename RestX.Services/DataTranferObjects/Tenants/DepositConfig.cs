using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Tenants
{
    public class DepositConfig
    {
        [Range(1, 100)]
        public int MinPartySize { get; set; }
        [Range(0, 999999999)]
        public decimal DepositAmountPerPerson { get; set; }
        [Range(1, 168)]
        public int DeadlineHours { get; set; }

        [Range(1, 720)]
        public int EarlyRefundHours { get; set; }
        [Range(0, 100)]
        public int EarlyRefundPercentage { get; set; }
        [Range(1, 720)]
        public int LateRefundHours { get; set; }
        [Range(0, 100)]
        public int LateRefundPercentage { get; set; }
    }
}

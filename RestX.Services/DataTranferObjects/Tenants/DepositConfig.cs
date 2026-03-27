using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Tenants
{
    public class DepositConfig
    {
        [Range(1, 100)]
        public int MinPartySize { get; set; } = 8;

        [Range(0, 999999999)]
        public decimal DepositAmountPerPerson { get; set; } = 50000;

        [Range(1, 168)]
        public int DeadlineHours { get; set; } = 2;

        [Range(1, 720)]
        public int EarlyRefundHours { get; set; } = 48;

        [Range(0, 100)]
        public int EarlyRefundPercentage { get; set; } = 100;

        [Range(1, 720)]
        public int LateRefundHours { get; set; } = 24;

        [Range(0, 100)]
        public int LateRefundPercentage { get; set; } = 50;
    }
}

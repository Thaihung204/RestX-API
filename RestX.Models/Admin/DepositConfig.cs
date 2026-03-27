using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestX.Models.Admin
{
    public class DepositConfig
    {
        [Key]
        public Guid TenantId { get; set; }
        public int MinPartySize { get; set; } = 8;
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmountPerPerson { get; set; } = 50000;
        public int DeadlineHours { get; set; } = 2;
        public int EarlyRefundHours { get; set; } = 48;
        [Range(0, 100)]
        public int EarlyRefundPercentage { get; set; } = 100;
        public int LateRefundHours { get; set; } = 24;
        [Range(0, 100)]
        public int LateRefundPercentage { get; set; } = 50;
    }
}

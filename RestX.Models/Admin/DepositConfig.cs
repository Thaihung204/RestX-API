using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestX.Models.Admin
{
    public class DepositConfig
    {
        [Key]
        public Guid TenantId { get; set; }
        public int MinPartySize { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmountPerPerson { get; set; }
        public int DeadlineHours { get; set; }
        public int EarlyRefundHours { get; set; }
        [Range(0, 100)]
        public int EarlyRefundPercentage { get; set; }
        public int LateRefundHours { get; set; }
        [Range(0, 100)]
        public int LateRefundPercentage { get; set; }
    }
}

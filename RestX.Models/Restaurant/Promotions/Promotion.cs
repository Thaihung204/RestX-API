using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Promotions
{
    public partial class Promotion : Entity<Guid>
    {
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal DiscountValue { get; set; } = 0;

        [MaxLength(20)]
        public string DiscountType { get; set; } = "PERCENTAGE";

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal MaxDiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal MinOrderAmount { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int UsageLimit { get; set; } = 0;

        [Range(0, 100)]
        public int UsagePerCustomer { get; set; } = 1;

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<PromotionApplicableItem> PromotionApplicableItems { get; set; } = new HashSet<PromotionApplicableItem>();
        public virtual ICollection<PromotionHistory> PromotionHistories { get; set; } = new HashSet<PromotionHistory>();
    }
}

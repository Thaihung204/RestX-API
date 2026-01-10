using RestX.Models.BaseModel;
using RestX.Models.Promotions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Menu
{
    public partial class MealCombo : Entity<Guid>
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        [Url]
        public string? ImageUrl { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal BaseCost { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<ComboDetail> ComboDetails { get; set; } = new HashSet<ComboDetail>();
        public virtual ICollection<PromotionApplicableItem> PromotionApplicableItems { get; set; } = new HashSet<PromotionApplicableItem>();
    }
}

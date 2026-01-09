using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Loyalty
{
    public partial class LoyaltyPointBand : Entity<Guid>
    {
        [Required]
        [MaxLength(20)]
        public string Name { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Min { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int? Max { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; } = 0;

        [Required]
        [MaxLength(500)]
        public string BenefitDescription { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}

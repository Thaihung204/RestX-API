using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Loyalty
{
    public class LoyaltyPointBandItem
    {
        public Guid? Id { get; set; }
        [Required]
        [MaxLength(20)]
        public string Name { get; set; } = string.Empty;
        [Range(0, int.MaxValue)]
        public int Min { get; set; } = 0;
        [Range(0, int.MaxValue)]
        public int? Max { get; set; }
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; } = 0;
        [Required]
        [MaxLength(500)]
        public string BenefitDescription { get; set; } = string.Empty;
        [MaxLength(10)]
        public string LogoColor { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}

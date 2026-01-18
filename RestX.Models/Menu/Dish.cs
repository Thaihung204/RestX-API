using RestX.Models.BaseModel;
using RestX.Models.Orders;
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
    public partial class Dish : Entity<Guid>
    {
        public Guid CategoryId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string? Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;

        public bool IsVegetarian { get; set; } = false;

        public bool IsSpicy { get; set; } = false;

        public bool IsBestSeller { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public bool AutoDisableByStock { get; set; } = false;

        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<DishImage> DishImages { get; set; } = new HashSet<DishImage>();
        public virtual ICollection<DishRecipe> DishRecipes { get; set; } = new HashSet<DishRecipe>();
        public virtual ICollection<ComboDetail> ComboDetails { get; set; } = new HashSet<ComboDetail>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new HashSet<OrderDetail>();
        public virtual ICollection<PromotionApplicableItem> PromotionApplicableItems { get; set; } = new HashSet<PromotionApplicableItem>();
    }
}

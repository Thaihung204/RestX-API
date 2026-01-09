using RestX.Models.BaseModel;
using RestX.Models.Restaurant.Promotions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Menu
{
    public partial class Category : Entity<Guid>
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string? Description { get; set; } = string.Empty;

        [MaxLength(500)]
        [Url]
        public string? ImageUrl { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new HashSet<Category>();
        public virtual ICollection<Dish> Dishes { get; set; } = new HashSet<Dish>();
        public virtual ICollection<PromotionApplicableItem> PromotionApplicableItems { get; set; } = new HashSet<PromotionApplicableItem>();
    }
}

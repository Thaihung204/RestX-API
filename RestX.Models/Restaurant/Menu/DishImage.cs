using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Menu
{
    public partial class DishImage : Entity<Guid>
    {
        public Guid DishId { get; set; }

        [Required]
        [MaxLength(500)]
        [Url]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string TypeId { get; set; } = string.Empty;

        [Range(0, 999)]
        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public virtual Dish Dish { get; set; } = null!;
    }
}

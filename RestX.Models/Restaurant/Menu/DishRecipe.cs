using RestX.Models.BaseModel;
using RestX.Models.Restaurant.Inventory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Menu
{
    public partial class DishRecipe : Entity<Guid>
    {
        public Guid DishId { get; set; }
        public Guid IngredientId { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999999.999)]
        public decimal Quantity { get; set; }

        public virtual Dish Dish { get; set; } = null!;
        public virtual Ingredient Ingredient { get; set; } = null!;
    }
}

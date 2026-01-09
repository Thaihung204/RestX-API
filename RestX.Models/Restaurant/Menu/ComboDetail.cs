using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Menu
{
    public partial class ComboDetail : Entity<Guid>
    {
        public Guid ComboId { get; set; }
        public Guid DishId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public virtual MealCombo MealCombo { get; set; } = null!;
        public virtual Dish Dish { get; set; } = null!;
    }
}

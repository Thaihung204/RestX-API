using RestX.Models.BaseModel;
using RestX.Models.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Promotions
{
    public partial class PromotionApplicableItem : Entity<Guid>
    {
        public Guid PromotionId { get; set; }
        public Guid? DishId { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? ComboId { get; set; }

        public virtual Promotion Promotion { get; set; } = null!;
        public virtual Dish? Dish { get; set; }
        public virtual Category? Category { get; set; }
        public virtual MealCombo? Combo { get; set; }
    }
}

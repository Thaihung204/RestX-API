using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Dish
{
    public class DishRecipeItem
    {
        public Guid? Id { get; set; }
        public Guid DishId { get; set; }
        public Guid IngredientId { get; set; }
        public decimal Quantity { get; set; }
    }
}


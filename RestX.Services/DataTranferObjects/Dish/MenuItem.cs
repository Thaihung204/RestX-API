using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Dish
{
    public class MenuItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } = default!;

        public bool IsVegetarian { get; set; }
        public bool IsSpicy { get; set; }
        public bool IsBestSeller { get; set; }

        public string? ImageUrl { get; set; }

        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = default!;
    }

}

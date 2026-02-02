using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Dish
{
    public class MenuCategory
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = default!;
        public List<MenuItem> Items { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Promotion
{
    public class PromotionApplicableItem
    {
        public Guid? Id { get; set; }
        public Guid? DishId { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? ComboId { get; set; }

        public string? DishName { get; set; }
        public string? CategoryName { get; set; }
        public string? ComboName { get; set; }
    }
}

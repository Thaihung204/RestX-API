using RestX.Models.BaseModel;
using RestX.Models.Common;
using RestX.Models.Menu;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Orders
{
    public partial class OrderDetail : Entity<Guid>
    {
        public Guid OrderId { get; set; }
        public Guid DishId { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [MaxLength(500)]
        public string? Note { get; set; }

        public Guid ItemStatusId { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Dish Dish { get; set; } = null!;
        public virtual StatusValue ItemStatus { get; set; }
    }
}

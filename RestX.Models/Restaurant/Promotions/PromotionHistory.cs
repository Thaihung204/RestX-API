using RestX.Models.BaseModel;
using RestX.Models.Restaurant.Orders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Promotions
{
    public partial class PromotionHistory : Entity<Guid>
    {
        public Guid PromotionId { get; set; }
        public Guid OrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal DiscountAmount { get; set; } = 0;

        public virtual Promotion Promotion { get; set; } = null!;
        public virtual Order Order { get; set; } = null!;
    }
}

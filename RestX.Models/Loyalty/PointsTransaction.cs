using RestX.Models.BaseModel;
using RestX.Models.Customers;
using RestX.Models.Orders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Loyalty
{
    public partial class PointsTransaction : Entity<Guid>
    {
        public Guid CustomerId { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } = "EARN";

        public int Points { get; set; }

        public Guid OrderId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public virtual Customer Customer { get; set; } = null!;
        public virtual Order Order { get; set; }
    }
}

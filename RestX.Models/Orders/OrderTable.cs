using RestX.Models.BaseModel;
using RestX.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Orders
{
    public partial class OrderTable : Entity<Guid>
    {
        public Guid OrderId { get; set; }
        public Guid TableId { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Table Table { get; set; } = null!;
    }
}

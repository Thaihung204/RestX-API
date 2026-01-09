using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Common
{
    public partial class StatusType : Entity<Guid>
    {
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        public virtual ICollection<StatusValue> StatusValues { get; set; } = new HashSet<StatusValue>();
    }
}

using RestX.Models.BaseModel;
using RestX.Models.Restaurant.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Common
{
    public partial class StatusValue : Entity<Guid>
    {
        public Guid StatusTypeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string ColorCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public virtual StatusType StatusType { get; set; } = null!;
    }
}

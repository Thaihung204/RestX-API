using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Common
{
    public partial class StatusValue : Entity<int>
    {
        public int StatusTypeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string ColorCode { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;

        public int DisplayOrder { get; set; } = 1;
        public bool IsSystem { get; set; } = false;
        public virtual StatusType StatusType { get; set; } = null!;
    }
}

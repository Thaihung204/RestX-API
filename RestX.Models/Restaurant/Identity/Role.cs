using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Identity
{
    public partial class Role : Entity<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; } = string.Empty;

        public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; set; } = new HashSet<UserRoleAssignment>();
    }
}

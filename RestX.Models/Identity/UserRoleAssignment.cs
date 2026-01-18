using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Identity
{
    public partial class UserRoleAssignment : Entity<Guid>
    {
        public Guid ApplicationUserId { get; set; }
        public Guid RoleId { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
    }
}

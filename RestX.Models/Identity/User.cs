using RestX.Models.BaseModel;
using RestX.Models.Customers;
using RestX.Models.HR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Identity
{
    public partial class User : Entity<Guid>
    {
        [Required]
        [MaxLength(320)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        [Url]
        public string? Avatar { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; set; } = new HashSet<UserRoleAssignment>();
        public virtual ICollection<Employee> Employees { get; set; } = new HashSet<Employee>();
        public virtual ICollection<Customer> Customers { get; set; } = new HashSet<Customer>();
    }
}

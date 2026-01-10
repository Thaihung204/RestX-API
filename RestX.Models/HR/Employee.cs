using RestX.Models.BaseModel;
using RestX.Models.Identity;
using RestX.Models.Orders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.HR
{
    public partial class Employee : Entity<Guid>
    {

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [Required]
        [MaxLength(100)]
        public string Position { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime HireDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? TerminationDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999999.99)]
        public decimal Salary { get; set; }

        [Required]
        [MaxLength(20)]
        public string SalaryType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public virtual ApplicationUser ApplicationUser { get; set; } = null!;
        public virtual ICollection<EmployeeSchedule> EmployeeSchedules { get; set; } = new HashSet<EmployeeSchedule>();
        public virtual ICollection<Order> HandledOrders { get; set; } = new HashSet<Order>();
        public virtual ICollection<Payment> ProcessedPayments { get; set; } = new HashSet<Payment>();
    }
}

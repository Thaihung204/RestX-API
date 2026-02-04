using System.ComponentModel.DataAnnotations;
using RestX.BLL.DataTranferObjects.Common;
namespace RestX.BLL.DataTranferObjects.Employee
{
    public class EmployeeFilterParams : BaseFilterParams
    {
        public string? Position { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? HireDateFrom { get; set; }
        public DateTime? HireDateTo { get; set; }
    }
    public class CreateEmployee
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;
        [Phone(ErrorMessage = "Invalid phone number")]
        [MaxLength(15)]
        public string? PhoneNumber { get; set; }
        [MaxLength(500)]
        public string? Address { get; set; }
        [Required(ErrorMessage = "Position is required")]
        [MaxLength(100)]
        public string Position { get; set; } = string.Empty;
        [Required(ErrorMessage = "Hire date is required")]
        public DateTime HireDate { get; set; }
        [Range(0, 999999999999.99, ErrorMessage = "Salary must be a positive number")]
        public decimal Salary { get; set; } = 0;
        [MaxLength(20)]
        public string SalaryType { get; set; } = "Monthly";
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty; 
    }
    public class UpdateEmployee
    {
        [MaxLength(255)]
        public string? FullName { get; set; }
        [Phone(ErrorMessage = "Invalid phone number")]
        [MaxLength(15)]
        public string? PhoneNumber { get; set; }
        [MaxLength(20)]
        public string? Code { get; set; }
        [MaxLength(500)]
        public string? Address { get; set; }
        [MaxLength(100)]
        public string? Position { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        [Range(0, 999999999999.99, ErrorMessage = "Salary must be a positive number")]
        public decimal? Salary { get; set; }
        [MaxLength(20)]
        public string? SalaryType { get; set; }
        public bool? IsActive { get; set; }
    }
    public class EmployeeResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string Position { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public decimal Salary { get; set; }
        public string SalaryType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
    }
    public class EmployeeListItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

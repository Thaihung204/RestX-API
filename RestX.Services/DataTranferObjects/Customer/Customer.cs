using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using RestX.BLL.DataTranferObjects.Common;
namespace RestX.BLL.DataTranferObjects.Customer
{
    public class CustomerFilterParams : BaseFilterParams
    {
        public string? MembershipLevel { get; set; }
        public bool? IsActive { get; set; }
        public int? MinLoyaltyPoints { get; set; }
        public int? MaxLoyaltyPoints { get; set; }
    }
    public class CreateCustomer
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;
        [Phone(ErrorMessage = "Invalid phone number")]
        [MaxLength(15)]
        public string? PhoneNumber { get; set; }
        [MaxLength(20)]
        public string MembershipLevel { get; set; } = "BRONZE";
        public int LoyaltyPoints { get; set; } = 0;
        public IFormFile? Avatar { get; set; }
    }
    public class UpdateCustomer
    {
        [MaxLength(255)]
        public string? FullName { get; set; }
        [Phone(ErrorMessage = "Invalid phone number")]
        [MaxLength(15)]
        public string? PhoneNumber { get; set; }
        [MaxLength(20)]
        public string? MembershipLevel { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Loyalty points must be a positive number")]
        public int? LoyaltyPoints { get; set; }
        public bool? IsActive { get; set; }
        public IFormFile? Avatar { get; set; }
    }
    public class CustomerResponse
    {
        public Guid Id { get; set; }
        public string MembershipLevel { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public int TotalOrders { get; set; }
        public int TotalReservations { get; set; }
    }
    public class CustomerListItem
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string MembershipLevel { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AvatarUrl { get; set; }
    }
}

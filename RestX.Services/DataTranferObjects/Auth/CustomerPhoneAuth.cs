using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Auth
{
    public class CheckPhoneRequest
    {
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
    public class CheckPhoneResponse
    {
        public bool Exists { get; set; }
        public string? CustomerName { get; set; }
        public Guid? CustomerId { get; set; }
    }
    public class CustomerPhoneLoginRequest
    {
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
    public class CustomerPhoneRegisterRequest
    {
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string FullName { get; set; } = string.Empty;
    }
}

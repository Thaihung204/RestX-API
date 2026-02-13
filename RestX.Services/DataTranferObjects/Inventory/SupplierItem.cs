using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Inventory
{
    public class SupplierItem
    {
        public Guid? Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(15)]
        [Phone]
        public string? Phone { get; set; }

        [MaxLength(320)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
using RestX.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Inventory
{
    public class IngredientItem
    {
        public Guid? Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Range(0, 9999999.999)]
        public decimal MinStockLevel { get; set; } = 0;

        [Range(0, 9999999.999)]
        public decimal MaxStockLevel { get; set; } = 0;

        public Guid? SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Type { get; set; }

        public bool IsActive { get; set; } = true;

        public decimal CurrentQuantity { get; set; } = 0;

        public IngredientStatus Status { get; set; }
    }
}
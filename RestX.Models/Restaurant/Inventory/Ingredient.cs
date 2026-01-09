using RestX.Models.BaseModel;
using RestX.Models.Restaurant.Inventory;
using RestX.Models.Restaurant.Menu;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Inventory
{
    public partial class Ingredient : Entity<Guid>
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999999.999)]
        public decimal MinStockLevel { get; set; } = 0;

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999999.999)]
        public decimal MaxStockLevel { get; set; } = 0;

        public Guid? SupplierId { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Supplier? Supplier { get; set; }
        public virtual ICollection<DishRecipe> DishRecipes { get; set; } = new HashSet<DishRecipe>();
        public virtual InventoryStock? InventoryStock { get; set; }
        public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new HashSet<StockTransaction>();
    }
}

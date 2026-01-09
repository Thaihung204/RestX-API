using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Inventory
{
    public partial class InventoryStock : Entity<Guid>
    {
        public Guid IngredientId { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999999.999)]
        public decimal CurrentQuantity { get; set; } = 0;

        public DateTime? LastRestockDate { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public virtual Ingredient Ingredient { get; set; } = null!;
    }
}

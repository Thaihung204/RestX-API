using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Inventory
{
    public partial class StockTransaction : Entity<Guid>
    {
        public Guid IngredientId { get; set; }

        [MaxLength(20)]
        public string TransactionType { get; set; } = "IMPORT";

        [Column(TypeName = "decimal(10,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal UnitPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal TotalAmount { get; set; } = 0;

        [MaxLength(50)]
        public string? Reference { get; set; }

        public virtual Ingredient Ingredient { get; set; } = null!;
    }
}

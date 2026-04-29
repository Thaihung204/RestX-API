using RestX.Models.Attributes;
using RestX.Models.BaseModel;
using RestX.Models.Common;
using RestX.Models.Menu;
using System.ComponentModel.DataAnnotations;

namespace RestX.Models.Orders
{
    public partial class OrderDetail : Entity<Guid>
    {
        public Guid OrderId { get; set; }
        public Guid? DishId { get; set; }
        public Guid? ComboId { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [Range(0, 999999999.99)]
        public decimal UnitPrice { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        [TriggerProperty(DisplayName = "Status")]
        public int ItemStatusId { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Dish? Dish { get; set; }
        public virtual MealCombo? Combo { get; set; }
        public virtual StatusValue ItemStatus { get; set; } = null!;
    }
}
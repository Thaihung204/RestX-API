using RestX.BLL.DataTranferObjects.Table;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Orders
{
    public class OrderDetail
    {
        public Guid? Id { get; set; }

        public Guid? DishId { get; set; }
        public Guid? ComboId { get; set; }
        public Guid? ParentId { get; set; }

        public string? DishName { get; set; }
        public decimal? DishPrice { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [Range(0, 999999999.99)]
        public decimal UnitPrice { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public string? Status { get; set; }
        public Guid? OrderId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<string>? TableCode { get; set; }
    }
}
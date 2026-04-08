using RestX.BLL.DataTranferObjects.Table;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Orders
{
    public class OrderDetail
    {
        public Guid? Id { get; set; }

        [Required]
        public Guid DishId { get; set; }
        public string? DishName { get; set; }
        public decimal? DishPrice { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [MaxLength(500)]
        public string? Note { get; set; }

        public string? Status { get; set; }
        public Guid? OrderId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Order Order { get; set; }
    }
}
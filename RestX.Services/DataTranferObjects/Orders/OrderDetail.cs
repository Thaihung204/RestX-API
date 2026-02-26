using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Orders
{
    public class OrderDetail
    {
        public Guid? Id { get; set; }

        [Required]
        public Guid DishId { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [MaxLength(500)]
        public string? Note { get; set; }

        public int? StatusId { get; set; }
    }
}
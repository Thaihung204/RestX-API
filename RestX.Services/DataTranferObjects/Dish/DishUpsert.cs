using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Dish
{
    public class DishUpsert
    {
        public Guid? Id { get; set; } = null;
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public bool IsVegetarian { get; set; } = false;
        public bool IsSpicy { get; set; } = false;
        public bool IsBestSeller { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public bool AutoDisableByStock { get; set; } = false;
        public IFormFile? MainImage { get; set; }
        public List<IFormFile>? SubImages { get; set; }
    }
}

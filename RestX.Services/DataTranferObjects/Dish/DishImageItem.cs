using Microsoft.AspNetCore.Http;
using RestX.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Dish
{
    public class DishImageItem
    {
        public Guid? Id { get; set; }
        public IFormFile? File { get; set; }
        public string? ImageUrl { get; set; }
        public DishImageType? ImageType { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

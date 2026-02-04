using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Dish
{
    public class DishImageItem
    {
        public Guid Id { get; set; }
        public int DisplayOrder { get; set; }
        public string? ImageUrl { get; set; }
    }

}

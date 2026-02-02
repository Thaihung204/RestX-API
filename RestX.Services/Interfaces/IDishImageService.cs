using RestX.BLL.DataTranferObjects.Dish;
using RestX.Models.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Interfaces
{
    public interface IDishImageService
    {
        Task HandleDishImagesAsync(DishUpsert model, Dish dish);
        Task DeleteDishImageAsync(Guid dishImageId);
        Task DeleteAllByDishIdAsync(Guid dishId);

    }

}

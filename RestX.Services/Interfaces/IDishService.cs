using RestX.BLL.DataTranferObjects.Dish;
using RestX.Models.Menu;

namespace RestX.BLL.Interfaces
{
    public interface IDishService
    {
        Task<DishSearchResult> GetAllDishes(DishSearch model);
        Task<DishItem> GetDishById(Guid id);
        Task<Guid> UpsertDish(DishItem dishItem);
        Task<bool> DeleteDish(Guid id);
        Task<List<MenuCategory>> GetMenu();
    }
}
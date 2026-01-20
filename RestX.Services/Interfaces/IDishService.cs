using RestX.Models.Menu;

namespace RestX.BLL.Interfaces
{
    public interface IDishService
    {
        Task<IEnumerable<Dish>> GetAllDishes();
        Task<Dish?> GetDishById(Guid id);
        Task<Dish> UpsertDish(Dish model);
        Task DeleteDish(Guid id);
    }
}
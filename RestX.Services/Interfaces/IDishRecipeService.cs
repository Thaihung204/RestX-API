using RestX.BLL.DataTranferObjects.Dish;

namespace RestX.BLL.Interfaces
{
    public interface IDishRecipeService
    {
        Task<List<DishRecipeItem>> GetRecipesByDishId(Guid dishId);
        Task<DishRecipeItem?> GetRecipeById(Guid id);
        Task<Guid> CreateRecipe(DishRecipeItem item);
        Task<Guid> UpdateRecipe(Guid id, DishRecipeItem item);
        Task<bool> DeleteRecipe(Guid id);
        Task<Guid> SetRecipes(Guid dishId, List<DishRecipeItem> items);
    }
}

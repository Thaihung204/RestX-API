using RestX.BLL.DataTranferObjects.Combo;
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

        // Recipe methods
        Task<List<DishRecipeItem>> GetRecipesByDishId(Guid dishId);
        Task<DishRecipeItem?> GetRecipeById(Guid id);
        Task<Guid> CreateRecipe(DishRecipeItem item);
        Task<Guid> UpdateRecipe(Guid id, DishRecipeItem item);
        Task<bool> DeleteRecipe(Guid id);
        Task<Guid> SetRecipes(Guid dishId, List<DishRecipeItem> items);

        // Combo methods
        Task<List<ComboSummary>> GetAllCombos();
        Task<ComboSummary> GetComboById(Guid id);
        Task<Guid> UpsertCombo(ComboSummary comboSummary);
        Task<bool> DeleteCombo(Guid id);
        Task<List<ComboSummary>> GetActiveCombos();
    }
}
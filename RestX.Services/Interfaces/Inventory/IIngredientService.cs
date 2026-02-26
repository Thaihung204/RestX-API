using RestX.BLL.DataTranferObjects.Inventory;

namespace RestX.BLL.Interfaces.Inventory
{
    public interface IIngredientService
    {
        Task<IEnumerable<IngredientItem>> GetAllIngredients();
        Task<IngredientItem?> GetIngredientById(Guid id);
        Task<Guid> UpsertIngredient(IngredientItem ingredientItem);
        Task DeleteIngredient(Guid id);
        Task UpdateIngredientStatus(Guid id, decimal currentQuantity);
        Task<IEnumerable<IngredientCategory>> GetAllIngredientCategories();
        Task<IngredientCategory?> GetIngredientCategoryById(Guid id);
        Task<Guid> UpsertIngredientCategory(IngredientCategory model, string userId);
        Task<bool> DeleteIngredientCategory(Guid id);
    }
}

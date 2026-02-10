using RestX.BLL.DataTranferObjects.Inventory;

namespace RestX.BLL.Interfaces.Inventory
{
    public interface IIngredientService
    {
        Task<IEnumerable<IngredientItem>> GetAllIngredients();
        Task<IngredientItem?> GetIngredientById(Guid id);
        Task<Guid> UpsertIngredient(IngredientItem model);
        Task DeleteIngredient(Guid id);
    }
}

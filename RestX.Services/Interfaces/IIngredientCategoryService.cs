using RestX.BLL.DataTranferObjects.IngredientCategory;

namespace RestX.BLL.Interfaces
{
    public interface IIngredientCategoryService
    {
        Task<IEnumerable<IngredientCategories>> GetAllIngredientCategories();
        Task<IngredientCategories?> GetIngredientCategoryById(Guid id);
        Task<IngredientCategories> UpsertIngredientCategory(IngredientCategories model, string userName);
        Task DeleteIngredientCategory(Guid id);
    }
}

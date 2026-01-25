using RestX.Models.Menu;

namespace RestX.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllCategories();
        Task<Category?> GetCategoryById(Guid id);
        Task<Category> UpsertCategory(Category model);
        Task DeleteCategory(Guid id);
    }
}
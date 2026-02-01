using RestX.BLL.DataTranferObjects.Category;
using RestX.Models.Menu;

namespace RestX.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryItem>> GetAllCategories();
        Task<CategoryItem?> GetCategoryById(Guid id);
        Task<Category> UpsertCategory(Category model);
        Task DeleteCategory(Guid id);
    }
}
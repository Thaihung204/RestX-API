using RestX.BLL.DataTranferObjects.Category;

namespace RestX.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryItem>> GetAllCategories();
        Task<CategoryItem?> GetCategoryById(Guid id);
        Task<Guid> UpsertCategory(CategoryItem model);
        Task UpdateDisplayOrder(List<CategoryItem> categories);
        Task DeleteCategory(Guid id);
    }
}
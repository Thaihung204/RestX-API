using RestX.BLL.Interfaces;
using RestX.Models.Menu;

namespace RestX.BLL.Services
{
    public class CategoryService : BaseService, ICategoryService
    {
        private readonly IRepository repo;

        public CategoryService(IRepository repo) : base(repo)
        {
            this.repo = repo;
        }

        public async Task<IEnumerable<Category>> GetAllCategories()
        {
            return await repo.GetAllAsync<Category>(
                orderBy: q => q.OrderBy(c => c.Name),
                includeProperties: "ParentCategory,SubCategories");
        }

        public async Task<Category?> GetCategoryById(Guid id)
        {
            return await repo.GetOneAsync<Category>(
                filter: c => c.Id == id,
                includeProperties: "ParentCategory,SubCategories");
        }

        public async Task<Category> UpsertCategory(Category model)
        {
            if (model.Id != Guid.Empty)
            {
                var category = await repo.GetByIdAsync<Category>(model.Id);
                if (category == null)
                {
                    throw new InvalidOperationException("Category not found");
                }

                category.Name = model.Name;
                category.Description = model.Description;
                category.ImageUrl = model.ImageUrl;
                category.ParentId = model.ParentId;
                category.IsActive = model.IsActive;

                repo.Update(category);
                await repo.SaveAsync();

                return category;
            }

            await repo.CreateAsync(model);
            return model;
        }

        public async Task DeleteCategory(Guid id)
        {
            var category = await repo.GetByIdAsync<Category>(id);
            if (category == null)
            {
                return;
            }

            repo.Delete<Category>(id);
            await repo.SaveAsync();
        }
    }
}
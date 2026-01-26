using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using StackExchange.Redis;

namespace RestX.BLL.Services
{
    public class CategoryService : BaseService, ICategoryService
    {
        public CategoryService(
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
        }

        private string GetCacheKey()
            => $"{CurrentTenant?.Id}:category";

        public async Task<IEnumerable<Category>> GetAllCategories()
        {
            var categories = await this.RedisService.GetAsync<List<Category>>(GetCacheKey());
            if (categories == null)
            {
                categories = (await Repo.GetAllAsync<Category>(
                        orderBy: q => q.OrderBy(c => c.Name),
                        includeProperties: "ParentCategory,SubCategories"
                    )).ToList();

                await this.RedisService.SetAsync(GetCacheKey(), categories);
            }

            return categories;
        }

        public async Task<Category?> GetCategoryById(Guid id)
        {
            return await Repo.GetOneAsync<Category>(
                filter: c => c.Id == id,
                includeProperties: "ParentCategory,SubCategories"
            );
        }

        public async Task<Category> UpsertCategory(Category model)
        {
            if (model.Id != Guid.Empty)
            {
                var category = await Repo.GetByIdAsync<Category>(model.Id);
                if (category == null)
                    throw new InvalidOperationException("Category not found");

                category.Name = model.Name;
                category.Description = model.Description;
                category.ImageUrl = model.ImageUrl;
                category.ParentId = model.ParentId;
                category.IsActive = model.IsActive;

                Repo.Update(category);
                await Repo.SaveAsync();

                await RedisService.RemoveAsync(GetCacheKey());

                return category;
            }

            await Repo.CreateAsync(model);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync(GetCacheKey());

            return model;
        }

        public async Task DeleteCategory(Guid id)
        {
            var category = await Repo.GetByIdAsync<Category>(id);
            if (category == null)
                return;

            Repo.Delete<Category>(id);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync(GetCacheKey());
        }
    }
}

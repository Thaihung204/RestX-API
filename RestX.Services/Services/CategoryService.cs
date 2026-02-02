using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using RestX.BLL.DataTranferObjects.Category;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using StackExchange.Redis;

namespace RestX.BLL.Services
{
    public class CategoryService : BaseService, ICategoryService
    {
        private readonly IMapper mapper;
        public CategoryService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }

        private string GetCacheKey()
            => $"{CurrentTenant?.Id}:category";

        public async Task<IEnumerable<CategoryItem>> GetAllCategories()
        {
            var categories = await RedisService.GetAsync<List<Category>>(GetCacheKey());
            if (categories == null)
            {
                categories = (await Repo.GetAllAsync<Category>(
                    orderBy: q => q.OrderBy(c => c.Name),
                    includeProperties: "ParentCategory,SubCategories"
                )).ToList();

                await RedisService.SetAsync(GetCacheKey(), categories);
            }

            return mapper.Map<List<CategoryItem>>(categories);
        }
        public async Task<CategoryItem?> GetCategoryById(Guid id)
        {
            var category = await Repo.GetOneAsync<Category>(
                filter: c => c.Id == id,
                includeProperties: "ParentCategory,SubCategories"
            );
            return mapper.Map<CategoryItem>(category);
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

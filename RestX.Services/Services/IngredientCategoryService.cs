using AutoMapper;
using RestX.BLL.DataTranferObjects.IngredientCategory;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Inventory;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class IngredientCategoryService : BaseService, IIngredientCategoryService
    {
        private readonly IMapper mapper;
        public IngredientCategoryService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }
        private string GetCacheKey()
            => $"IngredientCategory:{CurrentTenant.Hostname}";

        public async Task<IEnumerable<IngredientCategories>> GetAllIngredientCategories()
        {
            var categories = await RedisService.GetAsync<List<IngredientCategory>>(GetCacheKey());
            if (categories == null)
            {
                categories = (await Repo.GetAllAsync<IngredientCategory>(
                    orderBy: q => q.OrderBy(c => c.Name)
                )).ToList();
                await RedisService.SetAsync(GetCacheKey(), categories);
            }
            return mapper.Map<List<IngredientCategories>>(categories);
        }

        public async Task<IngredientCategories?> GetIngredientCategoryById(Guid id)
        {
            var category = await Repo.GetOneAsync<IngredientCategory>(
                filter: c => c.Id == id
            );
            return mapper.Map<IngredientCategories>(category);
        }

        public async Task<IngredientCategories> UpsertIngredientCategory(IngredientCategories dto, string userName)
        {
            IngredientCategory category;
            if (dto.Id != null)
            {
                category = await Repo.GetByIdAsync<IngredientCategory>(dto.Id.Value);
                category.Name = dto.Name;
                category.Code = dto.Code;
                category.Description = dto.Description;
                category.IsActive = dto.IsActive;
                Repo.Update(category, userName);
                await Repo.SaveAsync();
                await RedisService.RemoveAsync(GetCacheKey());
                return mapper.Map<IngredientCategories>(category);
            }
            category = new IngredientCategory
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                IsActive = dto.IsActive
            };
            await Repo.CreateAsync(category, userName);
            await RedisService.RemoveAsync(GetCacheKey());
            return mapper.Map<IngredientCategories>(category);
        }

        public async Task DeleteIngredientCategory(Guid id)
        {
            var category = await Repo.GetByIdAsync<IngredientCategory>(id);
            if (category == null)
                return;
            Repo.Delete<IngredientCategory>(id);
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey());
        }
    }
}

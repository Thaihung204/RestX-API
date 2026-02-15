using AutoMapper;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Inventory;
using RestX.Models.Inventory;
using RestX.Models.Tenants;
using IngredientCategory = RestX.Models.Inventory.IngredientCategory;
using IngredientCategories = RestX.BLL.DataTranferObjects.Inventory.IngredientCategory;

namespace RestX.BLL.Services
{
    public class IngredientService : BaseService, IIngredientService
    {
        private readonly IMapper mapper;

        public IngredientService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }

        public async Task<IEnumerable<IngredientItem>> GetAllIngredients()
        {
                var ingredients = (await Repo.GetAllAsync<Ingredient>(
                    orderBy: q => q.OrderBy(i => i.Name),
                    includeProperties: "Supplier,InventoryStock"
                )).ToList();

            return mapper.Map<List<IngredientItem>>(ingredients);
        }

        public async Task<IngredientItem?> GetIngredientById(Guid id)
        {
            var ingredient = await Repo.GetOneAsync<Ingredient>(
                filter: i => i.Id == id,
                includeProperties: "Supplier,InventoryStock"
            );
            return mapper.Map<IngredientItem>(ingredient);
        }

        public async Task<Guid> UpsertIngredient(IngredientItem ingredientItem)
        {
            Ingredient ingredient;
            if (ingredientItem.Id != null)
            {
                ingredient = await Repo.GetByIdAsync<Ingredient>(ingredientItem.Id);
                if (ingredient == null)
                    return Guid.Empty;
                ingredient.Name = ingredientItem.Name;
                ingredient.Code = ingredientItem.Code;
                ingredient.Unit = ingredientItem.Unit;
                ingredient.MinStockLevel = ingredientItem.MinStockLevel;
                ingredient.MaxStockLevel = ingredientItem.MaxStockLevel;
                ingredient.SupplierId = ingredientItem.SupplierId;
                ingredient.Type = ingredientItem.Type;
                ingredient.IsActive = ingredientItem.IsActive;
                Repo.Update(ingredient);
                await Repo.SaveAsync();
                return ingredient.Id;
            }

            ingredient = new Ingredient
            {
                Name = ingredientItem.Name,
                Code = ingredientItem.Code,
                Unit = ingredientItem.Unit,
                MinStockLevel = ingredientItem.MinStockLevel,
                MaxStockLevel = ingredientItem.MaxStockLevel,
                SupplierId = ingredientItem.SupplierId,
                Type = ingredientItem.Type,
                IsActive = ingredientItem.IsActive
            };
            await Repo.CreateAsync(ingredient);
            return ingredient.Id;
        }

        public async Task DeleteIngredient(Guid id)
        {
            var ingredient = await Repo.GetByIdAsync<Ingredient>(id);
            if (ingredient == null)
                return;
            Repo.Delete<Ingredient>(id);
            await Repo.SaveAsync();
        }

        #region Ingredient Category
        private string GetCategoryCacheKey()
            => $"IngredientCategory:{CurrentTenant.Hostname}";

        public async Task<IEnumerable<IngredientCategories>> GetAllIngredientCategories()
        {
            var categories = await RedisService.GetAsync<List<IngredientCategory>>(GetCategoryCacheKey());
            if (categories == null)
            {
                categories = (await Repo.GetAllAsync<IngredientCategory>(
                    orderBy: q => q.OrderBy(c => c.Name)
                )).ToList();
                await RedisService.SetAsync(GetCategoryCacheKey(), categories);
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
                await RedisService.RemoveAsync(GetCategoryCacheKey());
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
            await RedisService.RemoveAsync(GetCategoryCacheKey());
            return mapper.Map<IngredientCategories>(category);
        }

        public async Task<bool> DeleteIngredientCategory(Guid id)
        {
            var category = await Repo.GetByIdAsync<IngredientCategory>(id);
            if (category == null)
                return false;
            var hasIngredients = await Repo.GetExistsAsync<Ingredient>(
                filter: i => i.IngredientCategoryId == id
            );
            if (hasIngredients)
                return false;
            Repo.Delete<IngredientCategory>(id);
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCategoryCacheKey());
            return true;
        }
        #endregion
    }
}
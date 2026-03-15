using AutoMapper;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Inventory;
using RestX.Models.Enum;
using RestX.Models.Inventory;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using IngredientCategories = RestX.BLL.DataTranferObjects.Inventory.IngredientCategory;
using IngredientCategory = RestX.Models.Inventory.IngredientCategory;

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
                ingredient = await Repo.GetOneAsync<Ingredient>(
                    filter: i => i.Id == ingredientItem.Id,
                    includeProperties: "InventoryStock"
                );

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
                ingredient.InventoryStock.CurrentQuantity = ingredientItem.CurrentQuantity;
                ingredient.InventoryStock.LastUpdated = DateTime.UtcNow;
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
                IsActive = ingredientItem.IsActive,
                InventoryStock = new InventoryStock
                {
                    CurrentQuantity = ingredientItem.CurrentQuantity,
                    LastUpdated = DateTime.UtcNow
                }
            };
            await Repo.CreateAsync(ingredient);
            return ingredient.Id;
        }

        public async Task UpdateIngredientStatus(Guid id, decimal currentQuantity)
        {
            var ingredient = await Repo.GetByIdAsync<Ingredient>(id);
            if (ingredient == null) return;
            ingredient.Status = currentQuantity == 0
                ? IngredientStatus.OutOfStock
                : ingredient.MinStockLevel > 0 && currentQuantity <= ingredient.MinStockLevel
                    ? IngredientStatus.LowStock
                    : IngredientStatus.InStock;
            Repo.Update(ingredient);
            await Repo.SaveAsync();
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
            => $"{CurrentTenant.Hostname}:IngredientCategory";

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

        public async Task<Guid> UpsertIngredientCategory(IngredientCategories request, string userId)
        {
            IngredientCategory category;
            if (request.Id != null)
            {
                category = await Repo.GetByIdAsync<IngredientCategory>(request.Id.Value);
                category.Name = request.Name;
                category.Code = request.Code;
                category.Description = request.Description;
                category.IsActive = request.IsActive;
                Repo.Update(category, userId);
                await Repo.SaveAsync();
                await RedisService.RemoveAsync(GetCategoryCacheKey());
                return category.Id;
            }
            category = new IngredientCategory
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                IsActive = request.IsActive
            };
            await Repo.CreateAsync(category, userId);
            await RedisService.RemoveAsync(GetCategoryCacheKey());
            return category.Id;
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

        public async Task DeductFromRecipe(Guid dishId, int quantity)
        {
            var recipes = await Repo.GetAsync<DishRecipe>(
                filter: r => r.DishId == dishId,
                includeProperties: "Ingredient,Ingredient.InventoryStock"
            );

            foreach (var recipe in recipes)
            {
                var ingredient = recipe.Ingredient;
                if (ingredient?.InventoryStock == null) continue;

                var deduction = recipe.Quantity * quantity;

                if (ingredient.InventoryStock.CurrentQuantity < deduction)
                {
                    throw new InvalidOperationException(
                        $"Not enough '{ingredient.Name}'. Avalablie Stock: {ingredient.InventoryStock.CurrentQuantity} {ingredient.Unit}"
                    );
                }
                ingredient.InventoryStock.CurrentQuantity -= deduction;

                await UpdateIngredientStatus(ingredient.Id, ingredient.InventoryStock.CurrentQuantity);
            }

            await Repo.SaveAsync();
        }

    }
}
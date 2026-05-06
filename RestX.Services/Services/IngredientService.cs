using AutoMapper;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.BLL.Exceptionhandling;
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
            await ValidateIngredientData(ingredientItem);

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
                ingredient.Unit = ingredientItem.Unit;
                ingredient.MinStockLevel = ingredientItem.MinStockLevel;
                ingredient.MaxStockLevel = ingredientItem.MaxStockLevel;
                ingredient.SupplierId = ingredientItem.SupplierId;
                ingredient.Type = ingredientItem.Type;
                ingredient.IsActive = ingredientItem.IsActive;

                if (ingredient.InventoryStock == null)
                {
                    ingredient.InventoryStock = new InventoryStock();
                }

                ingredient.InventoryStock.CurrentQuantity = ingredientItem.CurrentQuantity;
                ingredient.InventoryStock.LastUpdated = DateTime.UtcNow;

                if (ingredientItem.CurrentQuantity <= 0)
                {
                    ingredient.Status = IngredientStatus.OutOfStock;
                }
                else if (ingredientItem.CurrentQuantity < ingredient.MinStockLevel)
                {
                    ingredient.Status = IngredientStatus.LowStock;
                }
                else if (ingredientItem.CurrentQuantity > ingredient.MinStockLevel)
                {
                    ingredient.Status = IngredientStatus.InStock;
                }

                Repo.Update(ingredient);
                await Repo.SaveAsync();
                return ingredient.Id;
            }

            ingredient = new Ingredient
            {
                Name = ingredientItem.Name,
                Code = await GenerateNextIngredientCodeAsync(),
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

            if (ingredientItem.CurrentQuantity <= 0)
            {
                ingredient.Status = IngredientStatus.OutOfStock;
            }
            else if (ingredientItem.CurrentQuantity < ingredient.MinStockLevel)
            {
                ingredient.Status = IngredientStatus.LowStock;
            }
            else if (ingredientItem.CurrentQuantity > ingredient.MinStockLevel)
            {
                ingredient.Status = IngredientStatus.InStock;
            }

            await Repo.CreateAsync(ingredient);
            return ingredient.Id;
        }
        private async Task ValidateIngredientData(IngredientItem ingredientItem)
        {
            if (ingredientItem == null)
            {
                throw new AppException("Ingredient data is required.");
            }

            ingredientItem.Name = (ingredientItem.Name ?? string.Empty).Trim();
            ingredientItem.Unit = (ingredientItem.Unit ?? string.Empty).Trim();
            ingredientItem.Type = ingredientItem.Type?.Trim();

            if (string.IsNullOrWhiteSpace(ingredientItem.Name))
            {
                throw new AppException("Ingredient name is required.");
            }

            if (ingredientItem.Name.Length > 255)
            {
                throw new AppException("Ingredient name cannot exceed 255 characters.");
            }

            if (string.IsNullOrWhiteSpace(ingredientItem.Unit))
            {
                throw new AppException("Ingredient unit is required.");
            }

            if (ingredientItem.Unit.Length > 20)
            {
                throw new AppException("Ingredient unit cannot exceed 20 characters.");
            }

            if (!string.IsNullOrEmpty(ingredientItem.Type) && ingredientItem.Type.Length > 50)
            {
                throw new AppException("Ingredient type cannot exceed 50 characters.");
            }

            if (ingredientItem.MinStockLevel < 0 || ingredientItem.MaxStockLevel < 0)
            {
                throw new AppException("MinStockLevel and MaxStockLevel must be greater than or equal to 0.");
            }

            if (ingredientItem.MaxStockLevel > 0 && ingredientItem.MaxStockLevel < ingredientItem.MinStockLevel)
            {
                throw new AppException("MaxStockLevel must be greater than or equal to MinStockLevel.");
            }

            if (ingredientItem.CurrentQuantity < 0)
            {
                throw new AppException("CurrentQuantity must be greater than or equal to 0.");
            }

            if (ingredientItem.SupplierId.HasValue)
            {
                bool supplierExists = await Repo.GetExistsAsync<Supplier>(s => s.Id == ingredientItem.SupplierId.Value);
                if (!supplierExists)
                {
                    throw new AppException("Supplier not found.");
                }
            }

            string normalizedName = ingredientItem.Name.ToLower();
            bool duplicateName = await Repo.GetExistsAsync<Ingredient>(i =>
                i.Name.ToLower() == normalizedName
                && (!ingredientItem.Id.HasValue || i.Id != ingredientItem.Id.Value));

            if (duplicateName)
            {
                throw new AppException("Ingredient name already exists.");
            }
        }
        private async Task<string> GenerateNextIngredientCodeAsync()
        {
            int number = await Repo.GetCountAsync<Ingredient>() + 1;
            string code = $"ING{number:D3}";

            bool exists = await Repo.GetExistsAsync<Ingredient>(x => x.Code == code);
            while (exists)
            {
                number++;
                code = $"ING{number:D3}";
                exists = await Repo.GetExistsAsync<Ingredient>(x => x.Code == code);
            }

            return code;
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

        private async Task DisableDishesWhenIngredientsOutOfStock(HashSet<Guid> outOfStockIngredientIds)
        {
            if (!outOfStockIngredientIds.Any())
            {
                return;
            }

            List<DishRecipe> impactedRecipes = (await Repo.GetAsync<DishRecipe>(
                filter: r => outOfStockIngredientIds.Contains(r.IngredientId),
                includeProperties: "Dish"
            )).ToList();

            List<Dish> dishesToDisable = impactedRecipes
                .Where(r => r.Dish != null && r.Dish.AutoDisableByStock && r.Dish.IsActive)
                .Select(r => r.Dish!)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .ToList();

            foreach (Dish dish in dishesToDisable)
            {
                dish.IsActive = false;
                Repo.Update(dish);
            }
        }

        public async Task DeductFromRecipes(Dictionary<Guid, int> dishQuantities)
        {
            if (dishQuantities == null || !dishQuantities.Any())
            {
                return;
            }

            List<Guid> dishIds = dishQuantities
                .Where(x => x.Value > 0)
                .Select(x => x.Key)
                .Distinct()
                .ToList();

            if (!dishIds.Any())
            {
                return;
            }

            List<DishRecipe> recipes = (await Repo.GetAsync<DishRecipe>(
                filter: r => dishIds.Contains(r.DishId),
                includeProperties: "Ingredient,Ingredient.InventoryStock"
            )).ToList();

            Dictionary<Guid, decimal> deductionByIngredientId = new Dictionary<Guid, decimal>();
            Dictionary<Guid, Ingredient> ingredientsById = new Dictionary<Guid, Ingredient>();

            foreach (DishRecipe recipe in recipes)
            {
                if (!dishQuantities.TryGetValue(recipe.DishId, out int dishQuantity) || dishQuantity <= 0)
                {
                    continue;
                }

                Ingredient? ingredient = recipe.Ingredient;
                if (ingredient?.InventoryStock == null)
                {
                    continue;
                }

                decimal deduction = recipe.Quantity * dishQuantity;

                if (deductionByIngredientId.TryGetValue(ingredient.Id, out decimal currentDeduction))
                {
                    deductionByIngredientId[ingredient.Id] = currentDeduction + deduction;
                }
                else
                {
                    deductionByIngredientId[ingredient.Id] = deduction;
                }

                if (!ingredientsById.ContainsKey(ingredient.Id))
                {
                    ingredientsById[ingredient.Id] = ingredient;
                }
            }

            foreach (KeyValuePair<Guid, decimal> item in deductionByIngredientId)
            {
                Ingredient ingredient = ingredientsById[item.Key];
                decimal deduction = item.Value;

                if (ingredient.InventoryStock!.CurrentQuantity < deduction)
                {
                    throw new AppException(
                        $"Not enough '{ingredient.Name}'. Avalablie Stock: {ingredient.InventoryStock.CurrentQuantity} {ingredient.Unit}"
                    );
                }
            }

            HashSet<Guid> outOfStockIngredientIds = new HashSet<Guid>();

            foreach (KeyValuePair<Guid, decimal> item in deductionByIngredientId)
            {
                Ingredient ingredient = ingredientsById[item.Key];
                decimal deduction = item.Value;

                ingredient.InventoryStock!.CurrentQuantity -= deduction;
                ingredient.InventoryStock.LastUpdated = DateTime.UtcNow.AddHours(7);

                ingredient.Status = ingredient.InventoryStock.CurrentQuantity == 0
                    ? IngredientStatus.OutOfStock
                    : ingredient.MinStockLevel > 0 && ingredient.InventoryStock.CurrentQuantity <= ingredient.MinStockLevel
                        ? IngredientStatus.LowStock
                        : IngredientStatus.InStock;

                if (ingredient.InventoryStock.CurrentQuantity == 0)
                {
                    outOfStockIngredientIds.Add(ingredient.Id);
                }
            }

            await DisableDishesWhenIngredientsOutOfStock(outOfStockIngredientIds);
            await Repo.SaveAsync();
        }

        public async Task DeductFromRecipe(Guid dishId, int quantity)
        {
            if (quantity <= 0)
            {
                return;
            }

            Dictionary<Guid, int> dishQuantities = new Dictionary<Guid, int>
            {
                [dishId] = quantity
            };

            await DeductFromRecipes(dishQuantities);
        }
    }
}
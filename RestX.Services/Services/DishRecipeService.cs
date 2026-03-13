using AutoMapper;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.Interfaces;
using RestX.Models.Menu;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class DishRecipeService : BaseService, IDishRecipeService
    {
        private readonly IMapper mapper;

        public DishRecipeService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }

        public async Task<List<DishRecipeItem>> GetRecipesByDishId(Guid dishId)
        {
            var recipes = await Repo.GetAsync<DishRecipe>(
                filter: r => r.DishId == dishId,
                includeProperties: "Ingredient"
            );

            return mapper.Map<List<DishRecipeItem>>(recipes);
        }

        public async Task<DishRecipeItem?> GetRecipeById(Guid id)
        {
            var recipe = await Repo.GetByIdAsync<DishRecipe>(id);
            return recipe == null ? null : mapper.Map<DishRecipeItem>(recipe);
        }

        public async Task<Guid> CreateRecipe(DishRecipeItem item)
        {
            var recipe = new DishRecipe
            {
                DishId = item.DishId,
                IngredientId = item.IngredientId,
                Quantity = item.Quantity
            };

            await Repo.CreateAsync(recipe, null);
            return recipe.Id;
        }

        public async Task<Guid> UpdateRecipe(Guid id, DishRecipeItem item)
        {
            var recipe = await Repo.GetByIdAsync<DishRecipe>(id);
            if (recipe == null)
                return Guid.Empty;

            recipe.IngredientId = item.IngredientId;
            recipe.Quantity = item.Quantity;

            Repo.Update(recipe);
            await Repo.SaveAsync();

            return recipe.Id;
        }

        public async Task<bool> DeleteRecipe(Guid id)
        {
            var recipe = await Repo.GetByIdAsync<DishRecipe>(id);
            if (recipe == null)
                return false;

            Repo.Delete<DishRecipe>(id);
            await Repo.SaveAsync();

            return true;
        }
        public async Task<Guid> SetRecipes(Guid dishId, List<DishRecipeItem> items)
        {
            var existingRecipes = await Repo.GetAsync<DishRecipe>(filter: r => r.DishId == dishId);
            foreach (var recipe in existingRecipes)
            {
                Repo.Delete<DishRecipe>(recipe.Id);
            }

            foreach (var item in items)
            {
                var recipe = new DishRecipe
                {
                    DishId = dishId,
                    IngredientId = item.IngredientId,
                    Quantity = item.Quantity
                };
                await Repo.CreateAsync(recipe, null);
            }

            await Repo.SaveAsync();
            return dishId;
        }

    }
}

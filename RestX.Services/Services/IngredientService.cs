using AutoMapper;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Inventory;
using RestX.Models.Inventory;
using RestX.Models.Tenants;

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

            Repo.Create(ingredient);

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
    }
}
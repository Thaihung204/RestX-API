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

        private string GetCacheKey()
            => $"{CurrentTenant?.Id}:ingredient";

        public async Task<IEnumerable<IngredientItem>> GetAllIngredients()
        {
            var ingredients = await RedisService.GetAsync<List<Ingredient>>(GetCacheKey());
            if (ingredients == null)
            {
                ingredients = (await Repo.GetAllAsync<Ingredient>(
                    orderBy: q => q.OrderBy(i => i.Name),
                    includeProperties: "Supplier,InventoryStock"
                )).ToList();

                await RedisService.SetAsync(GetCacheKey(), ingredients);
            }

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

        public async Task<Guid> UpsertIngredient(IngredientItem dto)
        {
            Ingredient ingredient;

            if (dto.Id != null)
            {
                ingredient = await Repo.GetByIdAsync<Ingredient>(dto.Id.Value);
                if (ingredient == null)
                    return Guid.Empty;

                ingredient.Name = dto.Name;
                ingredient.Code = dto.Code;
                ingredient.Unit = dto.Unit;
                ingredient.MinStockLevel = dto.MinStockLevel;
                ingredient.MaxStockLevel = dto.MaxStockLevel;
                ingredient.SupplierId = dto.SupplierId;
                ingredient.Type = dto.Type;
                ingredient.IsActive = dto.IsActive;

                Repo.Update(ingredient);
                await Repo.SaveAsync();

                await RedisService.RemoveAsync(GetCacheKey());
                return ingredient.Id;
            }

            ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Code = dto.Code,
                Unit = dto.Unit,
                MinStockLevel = dto.MinStockLevel,
                MaxStockLevel = dto.MaxStockLevel,
                SupplierId = dto.SupplierId,
                Type = dto.Type,
                IsActive = dto.IsActive
            };

            Repo.Create(ingredient);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync(GetCacheKey());
            return ingredient.Id;
        }

        public async Task DeleteIngredient(Guid id)
        {
            var ingredient = await Repo.GetByIdAsync<Ingredient>(id);
            if (ingredient == null)
                return;

            Repo.Delete<Ingredient>(id);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync(GetCacheKey());
        }
    }
}
using RestX.BLL.Interfaces;
using RestX.Models.Menu;

namespace RestX.BLL.Services
{
    public class DishService : BaseService, IDishService
    {
        private readonly IRepository _repo;

        public DishService(IRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Dish>> GetAllDishes()
        {
            var dishes = await _repo.GetAllAsync<Dish>();
            return dishes.ToList();
        }

        public async Task<Dish?> GetDishById(Guid id)
        {
            return await _repo.GetByIdAsync<Dish>(id);
        }

        public async Task<Dish> UpsertDish(Dish model)
        {
            if (model.Id != Guid.Empty)
            {
                var dish = await _repo.GetByIdAsync<Dish>(model.Id);
                if (dish == null)
                {
                    throw new InvalidOperationException("Dish not found");
                }

                dish.CategoryId = model.CategoryId;
                dish.Name = model.Name;
                dish.Description = model.Description;
                dish.Price = model.Price;
                dish.Unit = model.Unit;
                dish.Quantity = model.Quantity;
                dish.IsVegetarian = model.IsVegetarian;
                dish.IsSpicy = model.IsSpicy;
                dish.IsBestSeller = model.IsBestSeller;
                dish.IsActive = model.IsActive;
                dish.AutoDisableByStock = model.AutoDisableByStock;

                _repo.Update(dish);
                await _repo.SaveAsync();

                return dish;
            }

            await _repo.CreateAsync(model);
            await _repo.SaveAsync();
            return model;
        }

        public async Task DeleteDish(Guid id)
        {
            var dish = await GetDishById(id);
            if (dish != null)
            {
                _repo.Delete<Dish>(id);
                await _repo.SaveAsync();
            }
        }
    }
}
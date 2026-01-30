using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.Interfaces;
using RestX.Models.Menu;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/dishes")]
    [ApiController]
    public class DishesController : BaseController
    {
        private readonly IDishService dishService;

        public DishesController(IDishService dishService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            this.dishService = dishService;
        }

        [HttpGet]

        public async Task<ActionResult<IEnumerable<Dish>>> GetAllDishes([FromQuery] DishSearch searchModel)
        {
            try
            {
                var dishes = await dishService.GetAllDishes(searchModel);
                return Ok(dishes);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Dish>> GetDishById([Required] Guid id)
        {
            try
            {
                var dish = await dishService.GetDishById(id);
                return Ok(dish);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditDish([Required] Guid id, [FromForm] DishUpsert dish)
        {
            try
            {
                dish.Id = id;
                return Ok(await dishService.UpsertDish(dish));
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Dish>> AddDish([FromForm] DishUpsert dish)
        {
            try
            {
                return Ok(await dishService.UpsertDish(dish));
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDish([Required] Guid id)
        {
            try
            {
                await dishService.DeleteDish(id);
                return Ok();
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}
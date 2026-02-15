using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/dishes")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class DishesController : BaseController
    {
        private readonly IDishService dishService;

        public DishesController(IDishService dishService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.dishService = dishService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult<IEnumerable<Dish>>> GetAllDishes([FromQuery] DishSearch searchModel)
        {
            try
            {
                var dishes = await dishService.GetAllDishes(searchModel);
                return Ok(dishes);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Dish>> GetDishById([Required] Guid id)
        {
            try
            {
                var dish = await dishService.GetDishById(id);
                return Ok(dish);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> EditDish([Required] Guid id, [FromForm] DishItem dish)
        {
            try
            {
                dish.Id = id;
                return Ok(await dishService.UpsertDish(dish));
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult<Dish>> AddDish([FromForm] DishItem dish)
        {
            try
            {
                return Ok(await dishService.UpsertDish(dish));
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteDish([Required] Guid id)
        {
            try
            {
                await dishService.DeleteDish(id);
                return Ok();
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("menu")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<MenuCategory>>> GetMenu()
        {
            try
            {
                var menu = await dishService.GetMenu();
                return Ok(menu);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }
    }
}
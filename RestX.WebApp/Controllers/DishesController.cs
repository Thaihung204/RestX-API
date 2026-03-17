using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.Exceptionhandling;
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
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<ActionResult<IEnumerable<Dish>>> GetAllDishes([FromQuery] DishSearch searchModel)
        {
            try
            {
                var dishes = await dishService.GetAllDishes(searchModel);
                return Ok(dishes);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
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
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
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
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
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
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
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
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
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
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}/recipes")]
        public async Task<ActionResult<List<DishRecipeItem>>> GetRecipesByDishId([Required] Guid id)
        {
            try
            {
                var recipes = await dishService.GetRecipesByDishId(id);
                return Ok(recipes);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("recipe/{id:guid}")]
        public async Task<ActionResult<DishRecipeItem>> GetRecipeById([Required] Guid id)
        {
            try
            {
                var recipe = await dishService.GetRecipeById(id);
                if (recipe == null)
                    return NotFound(new { success = false, message = "Recipe not found" });

                return Ok(recipe);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("recipe")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult<Guid>> CreateRecipe([FromBody] DishRecipeItem item)
        {
            try
            {
                var id = await dishService.CreateRecipe(item);
                return Ok(id);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("recipe/{id:guid}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> UpdateRecipe([Required] Guid id, [FromBody] DishRecipeItem item)
        {
            try
            {
                var result = await dishService.UpdateRecipe(id, item);
                if (result == Guid.Empty)
                    return NotFound(new { success = false, message = "Recipe not found" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("recipe/{id:guid}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteRecipe([Required] Guid id)
        {
            try
            {
                var result = await dishService.DeleteRecipe(id);
                if (!result)
                    return NotFound(new { success = false, message = "Recipe not found" });

                return Ok();
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("{dishId:guid}/recipes")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult<Guid>> SetRecipes([Required] Guid dishId, [FromBody] List<DishRecipeItem> items)
        {
            try
            {
                var id = await dishService.SetRecipes(dishId, items);
                return Ok(id);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}
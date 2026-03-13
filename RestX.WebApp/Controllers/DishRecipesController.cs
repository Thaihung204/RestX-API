using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/dish-recipes")]
    [ApiController]
    public class DishRecipesController : BaseController
    {
        private readonly IDishRecipeService dishRecipeService;

        public DishRecipesController(
            IDishRecipeService dishRecipeService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.dishRecipeService = dishRecipeService;
        }

        [HttpGet("dish/{dishId:guid}")]
        public async Task<ActionResult<List<DishRecipeItem>>> GetRecipesByDishId([Required] Guid dishId)
        {
            try
            {
                var recipes = await dishRecipeService.GetRecipesByDishId(dishId);
                return Ok(recipes);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DishRecipeItem>> GetRecipeById([Required] Guid id)
        {
            try
            {
                var recipe = await dishRecipeService.GetRecipeById(id);
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

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateRecipe([FromBody] DishRecipeItem item)
        {
            try
            {
                var id = await dishRecipeService.CreateRecipe(item);
                return Ok(id);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateRecipe([Required] Guid id, [FromBody] DishRecipeItem item)
        {
            try
            {
                var result = await dishRecipeService.UpdateRecipe(id, item);
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

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteRecipe([Required] Guid id)
        {
            try
            {
                var result = await dishRecipeService.DeleteRecipe(id);
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

        [HttpPost("dish/{dishId:guid}")]
        public async Task<ActionResult<Guid>> SetRecipes([Required] Guid dishId, [FromBody] List<DishRecipeItem> items)
        {
            try
            {
                var id = await dishRecipeService.SetRecipes(dishId, items);
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

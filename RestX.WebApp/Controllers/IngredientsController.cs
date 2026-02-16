using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Inventory;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/ingredients")]
    [ApiController]
    public class IngredientsController : BaseController
    {
        private readonly IIngredientService ingredientService;

        public IngredientsController(
            IIngredientService ingredientService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.ingredientService = ingredientService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientItem>>> GetAllIngredients()
        {
            try
            {
                var ingredients = await ingredientService.GetAllIngredients();
                return Ok(ingredients);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IngredientItem>> GetIngredientById([Required] Guid id)
        {
            try
            {
                var ingredient = await ingredientService.GetIngredientById(id);
                return Ok(ingredient);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditIngredient([Required] Guid id, [FromBody] IngredientItem ingredient)
        {
            try
            {
                ingredient.Id = id;
                var ingredientId = await ingredientService.UpsertIngredient(ingredient);
                return Ok(ingredientId);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> AddIngredient([FromBody] IngredientItem ingredient)
        {
            try
            {
                var ingredientId = await ingredientService.UpsertIngredient(ingredient);
                return Ok(ingredientId);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngredient([Required] Guid id)
        {
            try
            {
                await ingredientService.DeleteIngredient(id);
                return Ok();
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        #region Ingredient Category

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<IngredientCategory>>> GetAllIngredientCategories()
        {
            try
            {
                var categories = await ingredientService.GetAllIngredientCategories();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("categories/{id}")]
        public async Task<ActionResult<IngredientCategory>> GetIngredientCategoryById([Required] Guid id)
        {
            try
            {
                var category = await ingredientService.GetIngredientCategoryById(id);
                return Ok(category);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("categories")]
        public async Task<ActionResult<Guid>> AddIngredientCategory([FromBody] IngredientCategory category)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var result = await ingredientService.UpsertIngredientCategory(category, user?.Id.ToString());
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("categories/{id}")]
        public async Task<ActionResult<Guid>> EditIngredientCategory([Required] Guid id, [FromBody] IngredientCategory category)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                category.Id = id;
                var result = await ingredientService.UpsertIngredientCategory(category, user?.Id.ToString());
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteIngredientCategory([Required] Guid id)
        {
            try
            {
                var result = await ingredientService.DeleteIngredientCategory(id);
                if (!result)
                    return BadRequest("Cannot delete category. It may not exist or still has ingredients assigned.");
                return Ok();
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        #endregion
    }
}
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.IngredientCategory;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/ingredient-categories")]
    [ApiController]
    public class IngredientCategoriesController : BaseController
    {
        private readonly IIngredientCategoryService ingredientCategoryService;

        public IngredientCategoriesController(IIngredientCategoryService ingredientCategoryService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.ingredientCategoryService = ingredientCategoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientCategories>>> GetAllIngredientCategories()
        {
            try
            {
                var categories = await ingredientCategoryService.GetAllIngredientCategories();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IngredientCategories>> GetIngredientCategoryById([Required] Guid id)
        {
            try
            {
                var category = await ingredientCategoryService.GetIngredientCategoryById(id);
                return Ok(category);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<IngredientCategories>> EditIngredientCategory([Required] Guid id, [FromBody] IngredientCategories category)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                category.Id = id;
                var result = await ingredientCategoryService.UpsertIngredientCategory(category, user?.UserName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<IngredientCategories>> AddIngredientCategory([FromBody] IngredientCategories category)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var result = await ingredientCategoryService.UpsertIngredientCategory(category, user?.UserName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngredientCategory([Required] Guid id)
        {
            try
            {
                await ingredientCategoryService.DeleteIngredientCategory(id);
                return Ok();
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}

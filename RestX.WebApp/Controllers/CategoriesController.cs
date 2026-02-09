using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Interfaces;
using RestX.Models.Menu;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/categories")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService categoryService;

        public CategoriesController(ICategoryService categoryService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            this.categoryService = categoryService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Category>>> GetAllCategories()
        {
            try
            {
                var categories = await categoryService.GetAllCategories();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Category>> GetCategoryById([Required] Guid id)
        {
            try
            {
                var category = await categoryService.GetCategoryById(id);
                return Ok(category);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> EditCategory([Required] Guid id, [FromBody] Category category)
        {
            try
            {
                category.Id = id;
                return Ok(await categoryService.UpsertCategory(category));
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult<Category>> AddCategory([FromBody] Category category)
        {
            try
            {
                return Ok(await categoryService.UpsertCategory(category));
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteCategory([Required] Guid id)
        {
            try
            {
                await categoryService.DeleteCategory(id);
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
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Category;
using RestX.BLL.Interfaces;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService categoryService;

        public CategoriesController(ICategoryService categoryService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            this.categoryService = categoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryItem>>> GetAllCategories()
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
        public async Task<ActionResult<CategoryItem>> GetCategoryById([Required] Guid id)
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
        public async Task<IActionResult> EditCategory([Required] Guid id, [FromForm] CategoryItem category)
        {
            try
            {
                category.Id = id;
                var categoryId = await categoryService.UpsertCategory(category);
                return Ok(categoryId);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> AddCategory([FromForm] CategoryItem category)
        {
            try
            {
                var categoryId = await categoryService.UpsertCategory(category);
                return Ok(categoryId);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
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
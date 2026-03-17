using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Combo;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/combos")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CombosController : BaseController
    {
        private readonly IComboService comboService;

        public CombosController(IComboService comboService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.comboService = comboService;
        }

        // ==================== ADMIN ENDPOINTS ====================

        [HttpGet]
        [Authorize(Roles = "Admin,System Admin,Waiter")]
        public async Task<ActionResult> GetAllCombos([FromQuery] ComboSearch searchModel)
        {
            try
            {
                var combos = await comboService.GetAllCombos(searchModel);
                return Ok(combos);
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
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult> GetComboById([Required] Guid id)
        {
            try
            {
                var combo = await comboService.GetComboById(id);
                return Ok(combo);
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
        public async Task<ActionResult> AddCombo([FromForm] ComboSummary combo)
        {
            try
            {
                return Ok(await comboService.UpsertCombo(combo));
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
        public async Task<IActionResult> EditCombo([Required] Guid id, [FromForm] ComboSummary combo)
        {
            try
            {
                combo.Id = id;
                return Ok(await comboService.UpsertCombo(combo));
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
        public async Task<IActionResult> DeleteCombo([Required] Guid id)
        {
            try
            {
                var result = await comboService.DeleteCombo(id);
                if (!result)
                    return NotFound("Combo not found");
                return Ok(new { message = "Combo deleted successfully" });
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

        // ==================== CLIENT ENDPOINTS ====================

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult> GetActiveCombos()
        {
            try
            {
                var combos = await comboService.GetActiveCombos();
                return Ok(combos);
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
    }
}

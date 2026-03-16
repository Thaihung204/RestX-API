using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Loyalty;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Loyalty;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/loyalty-point-bands")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class LoyaltyPointBandsController : BaseController
    {
        private readonly ILoyaltyPointBandService loyaltyPointBandService;

        public LoyaltyPointBandsController(
            ILoyaltyPointBandService loyaltyPointBandService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.loyaltyPointBandService = loyaltyPointBandService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllLoyaltyPointBands()
        {
            try
            {
                var bands = await loyaltyPointBandService.GetAllLoyaltyPointBands();
                return Ok(new { success = true, data = bands });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to get loyalty point bands" });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLoyaltyPointBandById([Required] Guid id)
        {
            try
            {
                var band = await loyaltyPointBandService.GetLoyaltyPointBandById(id);
                if (band == null)
                    return NotFound(new { success = false, message = "Loyalty point band not found" });
                return Ok(new { success = true, data = band });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to get loyalty point band" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> AddLoyaltyPointBand([FromBody] LoyaltyPointBand request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                var user = await GetCurrentUserAsync();
                var id = await loyaltyPointBandService.UpsertLoyaltyPointBand(request, user?.Id.ToString());
                return Ok(new { success = true, message = "Loyalty point band created successfully", data = new { id } });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to create loyalty point band" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> EditLoyaltyPointBand([Required] Guid id, [FromBody] LoyaltyPointBand request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                request.Id = id;
                var user = await GetCurrentUserAsync();
                var resultId = await loyaltyPointBandService.UpsertLoyaltyPointBand(request, user?.Id.ToString());
                if (resultId == Guid.Empty)
                    return NotFound(new { success = false, message = "Loyalty point band not found" });
                return Ok(new { success = true, message = "Loyalty point band updated successfully", data = new { id = resultId } });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to update loyalty point band" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteLoyaltyPointBand([Required] Guid id)
        {
            try
            {
                var success = await loyaltyPointBandService.DeleteLoyaltyPointBand(id);
                if (!success)
                    return NotFound(new { success = false, message = "Loyalty point band not found" });
                return Ok(new { success = true, message = "Loyalty point band deleted successfully" });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to delete loyalty point band" });
            }
        }
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers
{
    [Route("api/deposit-config")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,System Admin")]
    public class DepositConfigController : BaseController
    {
        private readonly IDepositConfigService depositConfigService;

        public DepositConfigController(
            IDepositConfigService depositConfigService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.depositConfigService = depositConfigService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDepositConfig()
        {
            try
            {
                var config = await depositConfigService.GetDepositConfig(CurrentTenant.Id);
                if (config == null)
                    return NotFound(new { success = false, message = "Deposit config not found" });

                return Ok(new { success = true, data = config });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpsertDepositConfig([FromBody] DepositConfig config)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Validation failed", errors = ModelState });

                await depositConfigService.UpsertDepositConfig(CurrentTenant.Id, config);
                return Ok(new { success = true, message = "Deposit config saved" });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteDepositConfig()
        {
            try
            {
                await depositConfigService.DeleteDepositConfig(CurrentTenant.Id);
                return Ok(new { success = true, message = "Deposit config deleted" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
    }
}

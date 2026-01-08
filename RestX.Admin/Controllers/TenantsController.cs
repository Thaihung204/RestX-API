using Microsoft.AspNetCore.Mvc;
using RestX.Admin.Controllers.BaseControllers;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;
using System.ComponentModel.DataAnnotations;

namespace RestX.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantsController : BaseController
    {
        private readonly ITenantService tenantService;
        public readonly IExceptionHandler exceptionHandler;

        public TenantsController(ITenantService tenantService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            this.tenantService = tenantService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetAllTenants()
        {
            try
            {
                var tenants = await tenantService.GetAllTenants();
                return Ok(tenants);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetTenantById([Required] Guid id)
        {
            try
            {
                return Ok(await tenantService.GetTenantById(id));
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditTenant([Required] Guid id, [FromBody] Tenant tenant)
        {
            try
            {
                return Ok(await tenantService.UpsertTenant(tenant));
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Tenant>> AddTenant([FromBody] Tenant tenant)
        {
            try
            {
                return Ok(await tenantService.UpsertTenant(tenant));
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant([Required] Guid id)
        {
            try
            {
                await tenantService.DeleteTenant(id);
                return Ok();
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

    }
}

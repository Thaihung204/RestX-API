using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using RestX.Admin.Controllers.BaseControllers;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;
using System.ComponentModel.DataAnnotations;

namespace RestX.Admin.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    public class TenantController : BaseController
    {
        private readonly ITenantService tenantService;
        public readonly IExceptionHandler exceptionHandler;
        public TenantController(ITenantService tenantService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
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

        [HttpGet("{data}")]
        public async Task<ActionResult<Tenant>> GetTenantByIdOrHostname([Required] string data)
        {
            try
            {
                return Ok(await tenantService.GetTenantByIdOrHostname(data));
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> EditTenant([Required] Guid id, [FromForm] TenantItem tenant)
        {
            try
            {
                tenant.Id = id;
                return Ok(await tenantService.UpsertTenant(tenant));
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<TenantItem>> AddTenant([FromForm] TenantItem tenant)
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
        public async Task<IActionResult> DeleteTenant([Required] string id)
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

        [HttpGet("requests")]
        public async Task<ActionResult<IEnumerable<BLL.DataTranferObjects.Tenants.TenantRequest>>> GetAllTenantRequests()
        {
            try
            {
                return Ok(await tenantService.GetAllTenantRequests());
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("requests/{id:guid}")]
        public async Task<ActionResult<BLL.DataTranferObjects.Tenants.TenantRequest>> GetTenantRequestById([Required] Guid id)
        {
            try
            {
                return Ok(await tenantService.GetTenantRequestById(id));
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("requests")]
        public async Task<ActionResult<Guid>> AddTenantRequest([FromBody] BLL.DataTranferObjects.Tenants.TenantRequest tenantRequest)
        {
            try
            {
                return Ok(await tenantService.AddTenantRequest(tenantRequest));
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPatch("requests/{id:guid}/status")]
        public async Task<ActionResult<Guid>> ChangeTenantRequestStatus([Required] Guid id, [FromQuery] bool? isAccepted)
        {
            try
            {
                return Ok(await tenantService.ChangeStatus(id, isAccepted));
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("requests/{id:guid}")]
        public async Task<IActionResult> DeleteTenantRequest([Required] Guid id)
        {
            try
            {
                await tenantService.DeleteTenantRequest(id);
                return Ok();
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Services.Interfaces;
using RestX.Models.Tenants;
using System.ComponentModel.DataAnnotations;

namespace RestX.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants()
        {
            try
            {
                var tenants = await _tenantService.GetTenantsAsync();
                return Ok(tenants);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving tenants", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetTenant([Required] Guid id)
        {
            try
            {
                var tenant = await _tenantService.GetTenantByIdAsync(id);

                if (tenant == null)
                {
                    return NotFound(new { message = "Tenant not found" });
                }

                return Ok(tenant);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving tenant", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTenant([Required] Guid id, [FromBody] Tenant tenant)
        {
            if (id != tenant.Id)
            {
                return BadRequest(new { message = "ID in URL does not match ID in request body" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingTenant = await _tenantService.GetTenantByIdAsync(id);
                if (existingTenant == null)
                {
                    return NotFound(new { message = "Tenant not found" });
                }

                await _tenantService.UpsertTenantAsync(tenant);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error updating tenant", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Tenant>> PostTenant([FromBody] Tenant tenant)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                tenant.Id = Guid.Empty;

                var createdTenant = await _tenantService.UpsertTenantAsync(tenant);

                return CreatedAtAction(nameof(GetTenant), new { id = createdTenant.Id }, createdTenant);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error creating tenant", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant([Required] Guid id)
        {
            try
            {
                var existingTenant = await _tenantService.GetTenantByIdAsync(id);
                if (existingTenant == null)
                {
                    return NotFound(new { message = "Tenant not found" });
                }

                await _tenantService.DeleteTenantAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error deleting tenant", error = ex.Message });
            }
        }

    }
}

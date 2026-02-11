using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.StatusValue;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.StatusValues;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers;

[Route("api/status-values")]
[ApiController]
public class StatusValuesController : BaseController
{
    private readonly IStatusValueService statusValueService;

    public StatusValuesController(
        IStatusValueService statusValueService,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        IExceptionHandler exceptionHandler,
        IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
    {
        this.statusValueService = statusValueService;
    }

    [HttpGet("{typeCode}")]
    public async Task<ActionResult<IEnumerable<StatusValueItem>>> GetByType([FromRoute] string typeCode)
    {
        try
        {
            var data = await statusValueService.GetByType(typeCode);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpGet("{typeCode}/{id}")]
    public async Task<ActionResult<StatusValueItem>> GetById([FromRoute] string typeCode, [Required] int id)
    {
        try
        {
            var data = await statusValueService.GetById(id);
            if (data == null)
                return NotFound(new { success = false, message = "Status value not found" });

            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpPost("{typeCode}")]
    public async Task<ActionResult<StatusValueItem>> Create(
        [FromRoute] string typeCode,
        [FromBody] UpsertStatusValueRequest request)
    {
        try
        {
            var data = await statusValueService.Upsert(typeCode, null, request);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpPut("{typeCode}/{id}")]
    public async Task<ActionResult<StatusValueItem>> Update(
        [FromRoute] string typeCode,
        [Required] int id,
        [FromBody] UpsertStatusValueRequest request)
    {
        try
        {
            var data = await statusValueService.Upsert(typeCode, id, request);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpDelete("{typeCode}/{id}")]
    public async Task<IActionResult> Delete([FromRoute] string typeCode, [Required] int id)
    {
        try
        {
            await statusValueService.Delete(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }
}

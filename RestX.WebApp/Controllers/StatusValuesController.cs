using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Status;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Status;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers;

[Route("api/statuses")]
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
    public async Task<ActionResult<IEnumerable<StatusValues>>> GetStatusByType([FromRoute] string typeCode)
    {
        try
        {
            var data = await statusValueService.GetStatuses(typeCode);
            return Ok(new { success = true, data });
        }
        catch (AppException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpGet("{typeCode}/{id}")]
    public async Task<ActionResult<StatusValues>> GetStatusById([FromRoute] string typeCode, [Required] int id)
    {
        try
        {
            var data = await statusValueService.GetStatusValueById(id);
            if (data == null)
                return NotFound(new { success = false, message = "Status value not found" });
            return Ok(new { success = true, data });
        }
        catch (AppException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpPost("{typeCode}")]
    public async Task<ActionResult<StatusValues>> CreateStatus(
        [FromRoute] string typeCode,
        [FromBody] StatusValues request)
    {
        try
        {
            var data = await statusValueService.UpsertStatusValue(typeCode, null, request);
            return Ok(new { success = true, data });
        }
        catch (AppException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpPut("{typeCode}/{id}")]
    public async Task<ActionResult<StatusValues>> UpdateStatus(
        [FromRoute] string typeCode,
        [Required] int id,
        [FromBody] StatusValues request)
    {
        try
        {
            var data = await statusValueService.UpsertStatusValue(typeCode, id, request);
            return Ok(new { success = true, data });
        }
        catch (AppException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpDelete("{typeCode}/{id}")]
    public async Task<IActionResult> DeleteStatus([FromRoute] string typeCode, [Required] int id)
    {
        try
        {
            await statusValueService.DeleteStatusValue(typeCode, id);
            return Ok(new { success = true });
        }
        catch (AppException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }
}
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Share;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.StatusValues;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers;

[Route("api/forms")]
[ApiController]
public class FormController : BaseController
{
    private readonly IStatusValueService statusValueService;

    public FormController(
            IStatusValueService statusValueService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
    {
        this.statusValueService = statusValueService;
    }

    [HttpGet("get-lists/{name}")]
    public IActionResult GetLists([FromRoute] string name)
    {
        try
        {
            var data = FormListHelper.GetListByName(name);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }

    [HttpGet("get-status-lists/{typeCode}")]
    public async Task<IActionResult> GetStatusLists([FromRoute] string typeCode)
    {
        try
        {
            var values = await statusValueService.GetByType(typeCode);
            var data = values.Select(v => new SelectOption(v.Id.ToString(), v.Name)).ToList();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }
}
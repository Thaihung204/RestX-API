using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers;

[Route("api/forms")]
[ApiController]
public class FormController : BaseController
{
    public FormController(
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
    {
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
}
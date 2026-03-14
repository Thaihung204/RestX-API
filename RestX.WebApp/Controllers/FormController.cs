using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers;

[Route("api/forms")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class FormController : BaseController
{
    private readonly IFormListHelper formListHelper;

    public FormController(
            IFormListHelper formListHelper,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
    {
        this.formListHelper = formListHelper;
    }

    [HttpGet("get-lists/{name}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLists([FromRoute] string name)
    {
        try
        {
            var data = await formListHelper.GetListByName(name);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            this.ExceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }
}

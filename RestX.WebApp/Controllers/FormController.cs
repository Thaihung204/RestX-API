using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers;

[Route("api/forms")]
[ApiController]
public class FormController : BaseController
{
    public FormController(IExceptionHandler exceptionHandler) : base(exceptionHandler) { }

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
            exceptionHandler.RaiseException(ex);
            return BadRequest(new { success = false, message = "An internal error occurred" });
        }
    }
}
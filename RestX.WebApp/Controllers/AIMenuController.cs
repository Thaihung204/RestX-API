using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.AI;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RestX.WebApp.Controllers
{
    [Route("api/ai")]
    [ApiController]
    public class AIMenuController : BaseController
    {
        private readonly IAIMenuService _aiMenuService;

        public AIMenuController(
            IAIMenuService aiMenuService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            _aiMenuService = aiMenuService;
        }
        [HttpPost("chat")]
        [AllowAnonymous]
        public async Task<ActionResult<AIChatResponse>> Chat([FromBody] AIChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest("Message không được để trống.");

                request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var response = await _aiMenuService.ChatAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("chat/stream")]
        [AllowAnonymous]
        public async Task ChatStream([FromBody] AIChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Message không được để trống.");
                return;
            }

            request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _aiMenuService.ChatStreamAsync(request, Response);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 500;
                    await Response.WriteAsync(ex.Message);
                }
            }
        }

        [HttpDelete("chat/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ClearSession([Required] string sessionId)
        {
            try
            {
                await _aiMenuService.ClearSessionAsync(sessionId);
                return Ok();
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return StatusCode(500, "Có lỗi xảy ra khi xóa session.");
            }
        }
    }
}

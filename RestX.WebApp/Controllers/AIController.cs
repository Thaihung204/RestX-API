using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.AI;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.Security.Claims;

namespace RestX.WebApp.Controllers
{
    [Route("api/ai")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AIController : BaseController
    {
        private const string SessionCookieName = "restx_ai_session";
        private readonly IAIService _aiMenuService;

        public AIController(
            IAIService aiMenuService,
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
                request.SessionId = await _aiMenuService.ResolveSession(Request.Cookies[SessionCookieName], request.UserId);

                var response = await _aiMenuService.Chat(request);

                SetSessionCookie(response.SessionId);
                return Ok(response);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
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
            request.SessionId = await _aiMenuService.ResolveSession(Request.Cookies[SessionCookieName], request.UserId);

            SetSessionCookie(request.SessionId);

            try
            {
                await _aiMenuService.ChatStream(request, Response);
            }
            catch (AppException ex)
            {
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 400;
                    await Response.WriteAsync(ex.Message);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 400;
                    await Response.WriteAsync("An internal error occurred");
                }
            }
        }

        [HttpPost("chat/confirm-order")]
        [Authorize]
        public async Task<ActionResult<Guid>> ConfirmOrder([FromBody] AIOrderDraft draft)
        {
            try
            {
                var sessionId = Request.Cookies[SessionCookieName];
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var orderId = await _aiMenuService.ConfirmOrder(sessionId ?? string.Empty, userId, draft);
                return Ok(orderId);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("chat/history")]
        [AllowAnonymous]
        public async Task<ActionResult<ChatHistoryResponse>> GetHistory()
        {
            var sessionId = Request.Cookies[SessionCookieName];
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(userId))
                return Ok(new ChatHistoryResponse());

            try
            {
                var history = await _aiMenuService.GetHistory(sessionId, userId);
                return Ok(history ?? new ChatHistoryResponse());
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("chat")]
        [AllowAnonymous]
        public async Task<IActionResult> ClearSession()
        {
            var sessionId = Request.Cookies[SessionCookieName];
            if (string.IsNullOrEmpty(sessionId))
                return NoContent();

            try
            {
                await _aiMenuService.ClearSession(sessionId);
                Response.Cookies.Delete(SessionCookieName);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("content/generate")]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<ActionResult<ContentGenerateResponse>> GenerateContent([FromBody] ContentGenerateRequest request)
        {
            try
            {
                var response = await _aiMenuService.GenerateContent(request);
                return Ok(response);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("content/campaign-pack")]
        [Authorize(Roles = "Admin,System Admin, Staff")]
        public async Task<ActionResult<CampaignPackResponse>> GenerateCampaignPack([FromBody] CampaignPackRequest request)
        {
            try
            {
                var response = await _aiMenuService.GenerateCampaignPack(request);
                return Ok(response);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        // ─── Analytics ─────────────────────────────────────────────────────────

        [HttpPost("analytics")]
        //[Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult<AIAnalyticsResponse>> AnalyzeDashboard([FromBody] AIAnalyticsRequest request)
        {
            try
            {
                var response = await _aiMenuService.AnalyzeDashboard(request);
                return Ok(response);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        private void SetSessionCookie(string sessionId)
        {
            Response.Cookies.Append(SessionCookieName, sessionId, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                Secure = true,
                MaxAge = TimeSpan.FromMinutes(30)
            });
        }
    }
}

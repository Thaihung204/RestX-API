using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RestX.BLL.DataTranferObjects.Dashboard;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using RestX.WebApp.Helpers;

namespace RestX.WebApp.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin, System Admin")]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;
        private readonly IHubContext<SignalrServer> _hub;

        public DashboardController(
            IDashboardService dashboardService,
            IHubContext<SignalrServer> hub,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant)
            : base(mapper, userManager, exceptionHandler, tenant)
        {
            _dashboardService = dashboardService;
            _hub = hub;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] DashboardRequest request, [FromQuery] int top = 5, [FromQuery] string sortBy = "revenue")
        {
            try
            {
                var result = await _dashboardService.GetOverviewAsync(request, top, sortBy);
                await _hub.BroadcastToTenant(CurrentTenant.Id, SignalrServer.DashboardOverviewUpdated, result);
                return Ok(result);
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

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DashboardRequest request)
        {
            try
            {
                var result = await _dashboardService.GetSummaryAsync(request);
                return Ok(result);
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

        [HttpGet("revenue-trend")]
        public async Task<IActionResult> GetRevenueTrend([FromQuery] DashboardRequest request)
        {
            try
            {
                var result = await _dashboardService.GetRevenueTrendAsync(request);
                return Ok(result);
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

        [HttpGet("order-trend")]
        public async Task<IActionResult> GetOrderTrend([FromQuery] DashboardRequest request)
        {
            try
            {
                var result = await _dashboardService.GetOrderTrendAsync(request);
                return Ok(result);
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

        [HttpGet("top-dishes")]
        public async Task<IActionResult> GetTopDishes([FromQuery] DashboardRequest request, [FromQuery] int top = 5, [FromQuery] string sortBy = "revenue")
        {
            try
            {
                var result = await _dashboardService.GetTopDishesAsync(request, top, sortBy);
                return Ok(result);
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

        [HttpGet("table-status")]
        public async Task<IActionResult> GetTableStatus()
        {
            try
            {
                var result = await _dashboardService.GetTableStatusAsync();

                await _hub.BroadcastToTenant(CurrentTenant.Id, SignalrServer.DashboardTableStatusUpdated, result);

                return Ok(result);
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

        [HttpGet("customer-stats")]
        public async Task<IActionResult> GetCustomerStats([FromQuery] DashboardRequest request)
        {
            try
            {
                var result = await _dashboardService.GetCustomerStatsAsync(request);
                return Ok(result);
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
        [HttpPost("push-summary")]
        public async Task<IActionResult> PushSummary([FromQuery] DashboardRequest request)
        {
            try
            {
                var result = await _dashboardService.GetSummaryAsync(request);
                await _hub.BroadcastToTenant(CurrentTenant.Id, SignalrServer.DashboardSummaryUpdated, result);
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
    }
}

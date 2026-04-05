using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Dashboard;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin, System Admin")]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(
            IDashboardService dashboardService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant)
            : base(mapper, userManager, exceptionHandler, tenant)
        {
            _dashboardService = dashboardService;
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
        public async Task<IActionResult> GetTopDishes([FromQuery] DashboardRequest request,[FromQuery] int top = 5,[FromQuery] string sortBy = "revenue")
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
    }
}

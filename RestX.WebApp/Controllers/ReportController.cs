using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Reports;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin, System Admin")]
    public class ReportController : BaseController
    {
        private readonly IReportService _reportService;

        public ReportController(
            IReportService reportService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant)
            : base(mapper, userManager, exceptionHandler, tenant)
        {
            _reportService = reportService;
        }

        [HttpPost("export")]
        public async Task<IActionResult> Export([FromBody] ReportRequest request)
        {
            try
            {
                var pdfBytes = await _reportService.ExportAsync(request);

                var reportType = request.ReportType?.ToLower() ?? "monthly";
                var today = DateTime.UtcNow.AddHours(7);
                var filename = $"report_{reportType}_{today:yyyyMMdd_HHmm}.pdf";

                return File(pdfBytes, "application/pdf", filename);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred while generating the report.");
            }
        }

        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] ReportRequest request)
        {
            try
            {
                var data = await _reportService.PrepareDataAsync(request);
                return Ok(data);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred while preparing report data.");
            }
        }
    }
}

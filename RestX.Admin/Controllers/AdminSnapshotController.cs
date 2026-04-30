using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestX.Admin.Controllers.BaseControllers;
using RestX.BLL.DataTranferObjects.Admin;
using RestX.BLL.Interfaces;

namespace RestX.Admin.Controllers;

[Route("api/snapshots")]
[ApiController]
//[Authorize]
public class AdminSnapshotController : BaseController
{
    private readonly IAdminSnapshotService _snapshotService;

    public AdminSnapshotController(IAdminSnapshotService snapshotService, IExceptionHandler exceptionHandler)
        : base(exceptionHandler)
    {
        _snapshotService = snapshotService;
    }

    [HttpPost("trigger/seed")]
    public async Task<ActionResult> SeedMonth([FromQuery] DateOnly month)
    {
        try
        {
            await _snapshotService.SeedMonthSnapshotsAsync(month);
            return Ok($"Seeded snapshots for {month:yyyy-MM}.");
        }
        catch (Exception ex)
        {
            exceptionHandler.RaiseException(ex);
            return BadRequest("An internal error occurred");
        }
    }

    [HttpGet]
    public async Task<ActionResult<TenantSnapshotAggregateDto>> GetSnapshots(
        [FromQuery] string periodType = "daily",
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] Guid? tenantId = null)
    {
        try
        {
            var snapshots = await _snapshotService.GetSnapshotsAsync(periodType, fromDate, toDate, tenantId);

            var aggregate = new TenantSnapshotAggregateDto
            {
                PeriodStart = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)),
                PeriodType = periodType,
                TotalRevenue = snapshots.Sum(s => s.Revenue),
                TotalDiscountAmount = snapshots.Sum(s => s.DiscountAmount),
                TotalOrders = snapshots.Sum(s => s.TotalOrders),
                CompletedOrders = snapshots.Sum(s => s.CompletedOrders),
                CancelledOrders = snapshots.Sum(s => s.CancelledOrders),
                TotalCustomers = snapshots.Sum(s => s.TotalCustomers),
                NewCustomers = snapshots.Sum(s => s.NewCustomers),
                NewReservations = snapshots.Sum(s => s.NewReservations),
                Tenants = snapshots
            };

            return Ok(aggregate);
        }
        catch (Exception ex)
        {
            exceptionHandler.RaiseException(ex);
            return BadRequest("An internal error occurred");
        }
    }
}

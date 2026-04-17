using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Payments;
using RestX.BLL.DataTranferObjects.Reservation;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Reservations;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Enum;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using RestX.WebApp.Helpers;

namespace RestX.WebApp.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ReservationsController : BaseController
    {
        private readonly IReservationService reservationService;
        private readonly ITableService tableService;
        private readonly IDepositConfigService depositConfigService;
        private readonly IHubContext<SignalrServer> hub;
        public ReservationsController(
            IReservationService reservationService,
            ITableService tableService,
            IDepositConfigService depositConfigService,
            IHubContext<SignalrServer> hub,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.reservationService = reservationService;
            this.tableService = tableService;
            this.depositConfigService = depositConfigService;
            this.hub = hub;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Validation failed", errors = ModelState });

                var result = await reservationService.CreateReservation(request);
                return Ok(new { success = true, message = "Reservation created successfully", data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet("export/csv")]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<IActionResult> ExportReservationsCsv([FromQuery] ReservationFilterParams filter)
        {
            try
            {
                var bytes = await reservationService.ExportAsync(filter);
                var fileName = $"reservations_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmmss}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<IActionResult> GetReservations([FromQuery] ReservationFilterParams filter)
        {
            try
            {
                var result = await reservationService.GetReservations(filter);
                return Ok(new { success = true, data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyReservations([FromQuery] PaginationParams pagination)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized(new { success = false, message = "User not found" });
                var result = await reservationService.GetMyReservations(user.Id, pagination);
                return Ok(new { success = true, data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet("{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReservationByCode(string code)
        {
            try
            {
                var result = await reservationService.GetReservationByCode(code);
                if (result == null)
                    return NotFound(new { success = false, message = "Reservation not found" });

                return Ok(new { success = true, data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin,Staff,Customer")]
        public async Task<IActionResult> GetReservationById(Guid id)
        {
            try
            {
                var result = await reservationService.GetReservationById(id);
                if (result == null)
                    return NotFound(new { success = false, message = "Reservation not found" });

                return Ok(new { success = true, data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin,Staff,Customer")]
        public async Task<IActionResult> UpdateReservation(Guid id, [FromBody] UpdateReservationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Validation failed", errors = ModelState });

                var result = await reservationService.UpdateReservation(id, request);
                return Ok(new { success = true, message = "Reservation updated successfully", data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeReservationStatusRequest request)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var reservation = await reservationService.GetReservationById(id);
                await reservationService.ChangeStatus(id, request.StatusId, user?.Id.ToString());

                if (reservation != null)
                {
                    foreach (var tableInfo in reservation.Tables)
                    {
                        var updatedTable = await tableService.GetTableById(tableInfo.Id);
                        if (updatedTable != null)
                            await hub.BroadcastToTenant(CurrentTenant.Id, SignalrServer.TableStatusChanged, new
                            {
                                tableId = updatedTable.Id,
                                tableCode = updatedTable.Code,
                                floorId = updatedTable.FloorId,
                                status = (int)updatedTable.TableStatusId,
                                statusName = updatedTable.TableStatusId.ToString()
                            });
                    }
                }

                return Ok(new { success = true, message = "Reservation status updated successfully" });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPost("{code}/checkin")]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<IActionResult> CheckIn(string code)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var result = await reservationService.CheckIn(code, user?.Id.ToString());

                foreach (var tableInfo in result.Tables)
                {
                    await hub.BroadcastToTenant(CurrentTenant.Id, SignalrServer.TableStatusChanged, new
                    {
                        tableId = tableInfo.Id,
                        tableCode = tableInfo.Code,
                        status = (int)TableStatus.Occupied,
                        statusName = TableStatus.Occupied.ToString()
                    });
                }

                return Ok(result);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteReservation(Guid id)
        {
            try
            {
                await reservationService.DeleteReservation(id);
                return Ok(new { success = true, message = "Reservation deleted successfully" });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        // ── Deposit ──────────────────────────────────────────────────────────

        [HttpGet("{id:guid}/deposit")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDepositStatus(Guid id)
        {
            try
            {
                var result = await reservationService.GetDepositStatus(id);
                return Ok(new { success = true, data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPost("{id:guid}/deposit/pay")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateDepositPaymentLink(Guid id)
        {
            try
            {
                var checkoutUrl = await reservationService.CreateDepositPaymentLink(id);
                return Ok(new { success = true, data = new { checkoutUrl } });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPost("{id:guid}/deposit/confirm-cash")]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<IActionResult> ConfirmCashDeposit(Guid id, [FromBody] CashPaymentRequest request)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                await reservationService.ConfirmCashDeposit(id,request, user?.Id.ToString());
                return Ok(new { success = true, message = "Cash deposit confirmed" });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet("config")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> GetDepositConfig()
        {
            try
            {
                var config = await depositConfigService.GetDepositConfig(CurrentTenant.Id);
                if (config == null)
                    return NotFound(new { success = false, message = "Deposit config not found" });

                return Ok(new { success = true, data = config });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPut("config")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> UpsertDepositConfig([FromBody] DepositConfig config)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Validation failed", errors = ModelState });

                await depositConfigService.UpsertDepositConfig(CurrentTenant.Id, config);
                return Ok(new { success = true, message = "Deposit config saved" });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpDelete("config")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteDepositConfig()
        {
            try
            {
                await depositConfigService.DeleteDepositConfig(CurrentTenant.Id);
                return Ok(new { success = true, message = "Deposit config deleted" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

    }
}

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Reservation;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Reservations;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ReservationsController : BaseController
    {
        private readonly IReservationService reservationService;

        public ReservationsController(
            IReservationService reservationService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.reservationService = reservationService;
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
                return Ok(new { success = true, message = "Reservation created successfully", data = new { result.Id } });
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
                await reservationService.ChangeStatus(id, request.StatusId, user?.Id.ToString());
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
                await reservationService.CheckIn(code, user?.Id.ToString());
                return Ok(new { success = true, message = "Checked in successfully" });
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
        [Authorize(Roles = "Admin,System Admin,Staff,Customer")]
        public async Task<IActionResult> CancelReservation(Guid id)
        {
            try
            {
                await reservationService.CancelReservation(id);
                return Ok(new { success = true, message = "Reservation cancelled successfully" });
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
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
    }
}

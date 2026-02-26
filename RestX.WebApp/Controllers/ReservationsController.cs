using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Reservation;
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

                Guid? applicationUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await GetCurrentUserAsync();
                    applicationUserId = user?.Id;
                }

                var result = await reservationService.CreateReservation(request, applicationUserId);
                return Ok(new { success = true, message = "Reservation created successfully", data = result });
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
        [Authorize(Roles = "Admin,System Admin,Waiter")]
        public async Task<IActionResult> GetReservations([FromQuery] ReservationFilterParams filter)
        {
            try
            {
                var result = await reservationService.GetReservations(filter);
                return Ok(new { success = true, data = result });
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
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet("lookup")]
        [AllowAnonymous]
        public async Task<IActionResult> LookupReservation(
            [FromQuery] string confirmationCode,
            [FromQuery] string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(confirmationCode) || string.IsNullOrWhiteSpace(phone))
                    return BadRequest(new { success = false, message = "Confirmation code and phone are required" });

                var result = await reservationService.LookupReservation(confirmationCode, phone);
                if (result == null)
                    return NotFound(new { success = false, message = "Reservation not found" });

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet("check-availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability([FromQuery] CheckAvailabilityParams request)
        {
            try
            {
                if (request.TableIds == null || !request.TableIds.Any())
                    return BadRequest(new { success = false, message = "At least one table ID is required" });

                var result = await reservationService.CheckAvailabilityReservation(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin,Waiter,Customer")]
        public async Task<IActionResult> GetReservationById(Guid id)
        {
            try
            {
                var result = await reservationService.GetReservationById(id);
                if (result == null)
                    return NotFound(new { success = false, message = "Reservation not found" });

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin,Waiter,Customer")]
        public async Task<IActionResult> UpdateReservation(Guid id, [FromBody] UpdateReservationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Validation failed", errors = ModelState });

                var result = await reservationService.UpdateReservation(id, request);
                return Ok(new { success = true, message = "Reservation updated successfully", data = result });
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
        [Authorize(Roles = "Admin,System Admin,Waiter,Customer")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateReservationStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Validation failed", errors = ModelState });

                await reservationService.UpdateReservationStatus(id, request.StatusId);
                return Ok(new { success = true, message = "Reservation status updated successfully" });
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

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin,Waiter,Customer")]
        public async Task<IActionResult> CancelReservation(Guid id)
        {
            try
            {
                await reservationService.CancelReservation(id);
                return Ok(new { success = true, message = "Reservation cancelled successfully" });
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

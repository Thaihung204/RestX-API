using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using RestX.BLL.DataTranferObjects.Payments;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.WebApp.Controllers
{
    [Route("api/payments")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PaymentsController : BaseController
    {
        private readonly IPaymentService paymentService;

        public PaymentsController(
            IPaymentService paymentService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.paymentService = paymentService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Tenant Admin,Waiter")]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? method,
            [FromQuery] string? status)
        {
            try
            {
                var result = await paymentService.GetAllPayments(from, to, method, status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("orders/{orderId:guid}")]
        [Authorize(Roles = "Admin,Tenant Admin,Waiter")]
        public async Task<IActionResult> GetPaymentsByOrder([FromRoute] Guid orderId)
        {
            try
            {
                var result = await paymentService.GetPaymentsByOrder(orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin,Tenant Admin,Waiter")]
        public async Task<IActionResult> GetPaymentById([FromRoute] Guid id)
        {
            try
            {
                var result = await paymentService.GetPaymentById(id);
                if (result == null)
                    return NotFound(new { success = false, message = "Payment not found" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("orders/{orderId:guid}/cash")]
        [Authorize(Roles = "Admin,Tenant Admin,Waiter")]
        public async Task<IActionResult> PayByCash([FromRoute] Guid orderId, [FromBody] CashPaymentRequest request)
        {
            try
            {
                var result = await paymentService.PayByCash(orderId, request);
                return Ok(result);
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
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("orders/{orderId:guid}")]
        [Authorize(Roles = "Admin,Waiter,Customer")]
        public async Task<IActionResult> CreatePayOSLink([FromRoute] Guid orderId)
        {
            try
            {
                var result = await paymentService.CreatePayOSLink(orderId);
                return Ok(result);
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
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Tenant Admin,Waiter,Customer")]
        public async Task<IActionResult> CancelPayOSLink([FromRoute] Guid id, [FromQuery] string? reason)
        {
            try
            {
                await paymentService.CancelPayOSLink(id, reason);
                return Ok(new { success = true });
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
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook([FromBody] Webhook webhookBody)
        {
            try
            {
                await paymentService.HandleWebhook(webhookBody);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }
}

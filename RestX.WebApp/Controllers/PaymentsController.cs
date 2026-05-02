using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.DataTranferObjects.Payments;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Services;
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
        private readonly IOrderService orderService;

        public PaymentsController(
            IPaymentService paymentService,
            IOrderService orderService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.paymentService = paymentService;
            this.orderService = orderService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? method,
            [FromQuery] string? status,
            [FromQuery] string? purpose)
        {
            try
            {
                var result = await paymentService.GetAllPayments(from, to, method, status, purpose);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("orders/{orderId:guid}")]
        [Authorize(Roles = "Admin,System Admin,Staff,Customer")]
        public async Task<IActionResult> GetPaymentsByOrder([FromRoute] Guid orderId)
        {
            try
            {
                var result = await paymentService.GetPaymentsByOrder(orderId);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin,Staff,Customer")]
        public async Task<IActionResult> GetPaymentById([FromRoute] Guid id)
        {
            try
            {
                var result = await paymentService.GetPaymentById(id);
                if (result == null)
                    return NotFound(new { success = false, message = "Payment not found" });
                return Ok(result);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("orders/{orderId:guid}/cash")]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<IActionResult> PayByCash([FromRoute] Guid orderId, [FromBody] CashPaymentRequest request)
        {
            try
            {
                var discountRequest = new ApplyDiscountRequest
                {
                    PromotionCode = request.PromotionCode,
                    ApplyMembership = request.ApplyMembership
                };

                await orderService.ApplyDiscount(orderId, discountRequest);

                var user = await GetCurrentUserAsync();
                await paymentService.RecalculateOrderAmountForCheckout(orderId, user?.Id.ToString());

                var result = await paymentService.PayByCash(orderId, request, user?.Id.ToString());
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
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("orders/{orderId:guid}")]
        [Authorize(Roles = "Admin,System Admin,Staff,Customer")]
        public async Task<IActionResult> CreatePaymentLink([FromRoute] Guid orderId, [FromBody] ApplyDiscountRequest request)
        {
            try
            {
                await orderService.ApplyDiscount(orderId, request);

                var user = await GetCurrentUserAsync();
                await paymentService.RecalculateOrderAmountForCheckout(orderId, user?.Id.ToString());

                var isCustomer = User.IsInRole("Customer");
                var result = await paymentService.CreatePaymentLink(orderId, user?.Id.ToString(), isCustomer);
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
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin,Staff")]
        public async Task<IActionResult> CancelPaymentLink([FromRoute] Guid id, [FromQuery] string? reason)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                await paymentService.CancelPaymentLink(id, reason, user?.Id.ToString());
                return Ok(new { success = true });
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
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id:guid}/receipt")]
        [Authorize(Roles = "Admin,System Admin,Staff,Customer")]
        public async Task<IActionResult> GetReceipt([FromRoute] Guid id)
        {
            try
            {
                var pdf = await paymentService.GenerateReceiptAsync(id);
                return File(pdf, "application/pdf", $"receipt-{id}.pdf");
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
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessWebhook([FromBody] Webhook webhookBody)
        {
            try
            {
                await paymentService.HandleWebhook(webhookBody);
                return Ok(new { success = true });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }
}

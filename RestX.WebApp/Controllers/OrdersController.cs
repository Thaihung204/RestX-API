using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class OrdersController : BaseController
    {
        private readonly IOrderService orderService;

        public OrdersController(
            IOrderService orderService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.orderService = orderService;
        }

        [HttpGet]
        //[Authorize(Roles = "Admin,Kitchen Staff,Waiter")]
        [AllowAnonymous]
        public async Task<ActionResult<OrderSearchResult>> GetAllOrders([FromQuery] OrderSearch model)
        {
            try
            {
                return Ok(await orderService.GetAllOrders(model));
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]

        public async Task<ActionResult<Order>> GetOrderById([Required] Guid id)
        {
            try
            {
                var order = await orderService.GetOrderById(id);
                if (order == null)
                    return NotFound(new { success = false, message = "Order not found" });

                return Ok(order);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<Guid>> CreateOrder([FromBody] Order order)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                var userId = string.Empty;
                if (currentUser?.Id != null)
                    userId = currentUser.Id.ToString();
                else userId = order.CustomerId.ToString();

                var id = await orderService.CreateOrder(order, userId);
                if (id == Guid.Empty)
                    return BadRequest(new { success = false, message = "Create order failed" });

                return Ok(id);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Waiter")]
        public async Task<ActionResult<Guid>> UpdateOrder([Required] Guid id, [FromBody] Order order)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                var userId = string.Empty;
                if (currentUser?.Id != null)
                    userId = currentUser.Id.ToString();
                else userId = order.CustomerId.ToString();

                    var updatedId = await orderService.UpdateOrder(id, order, userId);
                if (updatedId == Guid.Empty)
                    return NotFound(new { success = false, message = "Order not found" });

                return Ok(updatedId);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Waiter")]
        public async Task<IActionResult> DeleteOrder([Required] Guid id)
        {
            try
            {
                await orderService.DeleteOrder(id);
                return Ok();
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}
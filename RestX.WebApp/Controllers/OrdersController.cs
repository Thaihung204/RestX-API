using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using RestX.WebApp.Helpers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class OrdersController : BaseController
    {
        private readonly IOrderService orderService;
        private readonly IHubContext<SignalrServer> hubContext;

        public OrdersController(
            IOrderService orderService,
            IHubContext<SignalrServer> hubContext,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.orderService = orderService;
            this.hubContext = hubContext;
        }

        [HttpGet]
        [Authorize(Roles = "System Admin,Admin,Kitchen Staff,Waiter,Customer")]
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
        [Authorize(Roles = "System Admin,Admin,Kitchen Staff,Waiter,Customer")]
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
        [Authorize(Roles = "System Admin,Admin,Kitchen Staff,Waiter,Customer")]
        public async Task<ActionResult<Guid>> CreateOrder([FromBody] Order order)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                var userId = string.Empty;
                if (currentUser?.Id != null)
                    userId = currentUser.MemberId.ToString();
                else userId = order.CustomerId.ToString();

                var id = await orderService.CreateOrder(order, userId);
                if (id == Guid.Empty)
                    return BadRequest(new { success = false, message = "Create order failed" });
                var createdOrder = await orderService.GetOrderById(id);

                await BroadcastToTenant(SignalrServer.OrderCreated, new { id, order = createdOrder });

                return Ok(id);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "System Admin,Admin,Waiter")]
        public async Task<ActionResult<Guid>> UpdateOrder([Required] Guid id, [FromBody] Order order)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                var userId = string.Empty;
                if (currentUser?.Id != null)
                    userId = currentUser.MemberId.ToString();
                else userId = order.CustomerId.ToString();

                    var updatedId = await orderService.UpdateOrder(id, order, userId);
                if (updatedId == Guid.Empty)
                    return NotFound(new { success = false, message = "Order not found" });
                var updatedOrder = await orderService.GetOrderById(id);

                await BroadcastToTenant(SignalrServer.OrderUpdated, new { id = updatedId, order = updatedOrder });

                return Ok(updatedId);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "System Admin,Admin,Waiter")]
        public async Task<IActionResult> DeleteOrder([Required] Guid id)
        {
            try
            {
                await orderService.DeleteOrder(id);
                await BroadcastToTenant(SignalrServer.OrderDeleted, new { id });
                return Ok();
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "System Admin,Admin,Waiter,Kitchen Staff")]
        public async Task<ActionResult<bool>> UpdateOrderStatus([Required] Guid id, [FromBody] int statusId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                var userId = currentUser?.MemberId.ToString() ?? string.Empty;

                var result = await orderService.UpdateStatus(id, statusId, userId);
                if (!result)
                    return NotFound(new { success = false, message = "Order not found" });

                var updatedOrder = await orderService.GetOrderById(id);

                await BroadcastToTenant(SignalrServer.OrderUpdated, new { id, order = updatedOrder });

                return Ok(result);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{orderId:guid}/order-details-status/{detailId:guid}")]
        [Authorize(Roles = "System Admin,Admin,Waiter,Kitchen Staff")]
        public async Task<ActionResult<bool>> UpdateOrderDetailStatus([Required] Guid orderId, [Required] Guid detailId, [FromBody] int statusId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                var userId = currentUser?.MemberId.ToString() ?? string.Empty;

                var result = await orderService.UpdateOrderDetailStatus(detailId, statusId, userId);
                if (!result)
                    return NotFound(new { success = false, message = "Order detail not found" });

                var updatedOrder = await orderService.GetOrderById(orderId);
                await BroadcastToTenant(SignalrServer.OrderUpdated, new { id = orderId, order = updatedOrder });

                return Ok(result);
            }
            catch (Exception ex)
            {
                ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        private Task BroadcastToTenant(string eventName, object payload)
        {
            var group = CurrentTenant?.Id != Guid.Empty
                ? $"tenant_{CurrentTenant!.Id}"
                : "tenant_default";

            return hubContext.Clients.Group(group).SendAsync(eventName, payload);
        }
    }
}
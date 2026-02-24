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
        public async Task<ActionResult<IEnumerable<OrderItem>>> GetAllOrders()
        {
            try
            {
                return Ok(await orderService.GetAllOrders());
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderItem>> GetOrderById([Required] Guid id)
        {
            try
            {
                var order = await orderService.GetOrderById(id);
                return Ok(order);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateOrder([FromBody] OrderItem order)
        {
            try
            {
                var id = await orderService.UpsertOrder(order);
                return Ok(id);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<Guid>> UpdateOrder([Required] Guid id, [FromBody] OrderItem order)
        {
            try
            {
                order.Id = id;
                return Ok(await orderService.UpsertOrder(order));
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteOrder([Required] Guid id)
        {
            try
            {
                await orderService.DeleteOrder(id);
                return Ok();
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost("customer")]
        public async Task<ActionResult<OrderItem>> CreateOrderByCustomer([FromBody] OrderItem order)
        {
            try
            {
                var result = await orderService.CreateOrder(order);
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}
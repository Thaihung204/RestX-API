using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Services;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/promotions")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PromotionsController : BaseController
    {
        private readonly IPromotionService promotionService;
        private readonly IOrderService orderService;

        public PromotionsController(
            IPromotionService promotionService,
            IOrderService orderService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.promotionService = promotionService;
            this.orderService = orderService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,System Admin,Waiter")]
        public async Task<ActionResult> GetAllPromotions()
        {
            try
            {
                List<BLL.DataTranferObjects.Promotion.Promotion> promotions = await promotionService.GetAllPromotions();
                return Ok(promotions);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin,Waiter")]
        public async Task<ActionResult> GetPromotionById([Required] Guid id)
        {
            try
            {
                BLL.DataTranferObjects.Promotion.Promotion promotion = await promotionService.GetPromotionById(id);
                return Ok(promotion);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult> GetActivePromotions([FromQuery] Guid? orderId = null)
        {
            try
            {
                Guid? userId = (await GetCurrentUserAsync())?.Id;

                if (orderId.HasValue && orderId.Value != Guid.Empty)
                {
                    var order = await orderService.GetOrderById(orderId.Value);
                    if (order == null)
                    {
                        return NotFound(new { success = false, message = "Order not found" });
                    }

                    userId = order.Customer?.UserId;
                }

                List<BLL.DataTranferObjects.Promotion.Promotion> promotions =
                    await promotionService.GetActivePromotions(userId);

                return Ok(promotions);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("code/{code}")]
        [AllowAnonymous]
        public async Task<ActionResult> GetPromotionByCode([Required] string code)
        {
            try
            {
                BLL.DataTranferObjects.Promotion.Promotion? promotion = await promotionService.GetPromotionByCode(code);
                if (promotion == null)
                {
                    return NotFound("Promotion not found or inactive");
                }

                return Ok(promotion);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult> AddPromotion([FromBody] BLL.DataTranferObjects.Promotion.Promotion item)
        {
            try
            {
                Guid id = await promotionService.UpsertPromotion(item);
                return Ok(id);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult> EditPromotion([Required] Guid id, [FromBody] BLL.DataTranferObjects.Promotion.Promotion item)
        {
            try
            {
                item.Id = id;
                Guid result = await promotionService.UpsertPromotion(item);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeletePromotion([Required] Guid id)
        {
            try
            {
                bool result = await promotionService.DeletePromotion(id);
                if (!result)
                {
                    return NotFound("Promotion not found");
                }

                return Ok();
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }
    }
}
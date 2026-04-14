using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RestX.BLL.DataTranferObjects;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using RestX.WebApp.Helpers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class NotificationsController : BaseController
    {
        private readonly INotificationService notificationService;
        private readonly IHubContext<SignalrServer> hubContext;

        public NotificationsController(
            INotificationService notificationService,
            IHubContext<SignalrServer> hubContext,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.notificationService = notificationService;
            this.hubContext = hubContext;
        }

        [HttpGet]
        [Authorize(Roles = "System Admin,Admin,Staff")]
        public async Task<ActionResult<List<RestaurantNotification>>> GetAllNotifications()
        {
            try
            {
                return Ok(await notificationService.GetAllNotifications());
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

        [HttpGet("my")]
        [Authorize(Roles = "System Admin,Admin,Staff,Customer")]
        public async Task<ActionResult<List<RestaurantNotification>>> GetMyNotifications()
        {
            try
            {
                ApplicationUser currentUser = await GetCurrentUserAsync();
                string? recipientId = currentUser?.Id.ToString();

                return Ok(await notificationService.GetMyNotifications(recipientId));
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
        [Authorize(Roles = "System Admin,Admin,Staff,Customer")]
        public async Task<ActionResult<RestaurantNotification>> GetNotificationById([Required] Guid id)
        {
            try
            {
                RestaurantNotification? item = await notificationService.GetNotificationById(id);
                if (item == null)
                {
                    return NotFound(new { success = false, message = "Notification not found" });
                }

                return Ok(item);
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

        [HttpPost]
        [Authorize(Roles = "System Admin,Admin,Staff")]
        public async Task<ActionResult<RestaurantNotification>> CreateNotification([FromBody] RestaurantNotification item)
        {
            try
            {
                ApplicationUser currentUser = await GetCurrentUserAsync();
                string userId = currentUser?.Id.ToString() ?? string.Empty;

                RestaurantNotification created = await notificationService.CreateNotification(item, userId);

                await hubContext.BroadcastToTenant(
                    CurrentTenant.Id,
                    SignalrServer.NotificationCreated,
                    new { id = created.Id, notification = created });

                return Ok(created);
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

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "System Admin,Admin,Staff")]
        public async Task<ActionResult<Guid>> UpdateNotification([Required] Guid id, [FromBody] RestaurantNotification item)
        {
            try
            {
                ApplicationUser currentUser = await GetCurrentUserAsync();
                string userId = currentUser?.Id.ToString() ?? string.Empty;

                Guid updatedId = await notificationService.UpdateNotification(id, item, userId);
                if (updatedId == Guid.Empty)
                {
                    return NotFound(new { success = false, message = "Notification not found" });
                }

                RestaurantNotification? updated = await notificationService.GetNotificationById(updatedId);

                await hubContext.BroadcastToTenant(
                    CurrentTenant.Id,
                    SignalrServer.NotificationUpdated,
                    new { id = updatedId, notification = updated });

                return Ok(updatedId);
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

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "System Admin,Admin,Staff")]
        public async Task<IActionResult> DeleteNotification([Required] Guid id)
        {
            try
            {
                bool success = await notificationService.DeleteNotification(id);
                if (!success)
                {
                    return NotFound(new { success = false, message = "Notification not found" });
                }

                await hubContext.BroadcastToTenant(
                    CurrentTenant.Id,
                    SignalrServer.NotificationDeleted,
                    new { id });

                return Ok();
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

        [HttpPatch("{id:guid}/publish/{isPublished}")]
        [Authorize(Roles = "System Admin,Admin,Staff")]
        public async Task<ActionResult<bool>> SetPublishStatus([Required] Guid id, [Required] bool isPublished)
        {
            try
            {
                ApplicationUser currentUser = await GetCurrentUserAsync();
                string userId = currentUser?.Id.ToString() ?? string.Empty;

                bool success = await notificationService.SetPublishStatus(id, isPublished, userId);
                if (!success)
                {
                    return NotFound(new { success = false, message = "Notification not found" });
                }

                RestaurantNotification? updated = await notificationService.GetNotificationById(id);

                await hubContext.BroadcastToTenant(
                    CurrentTenant.Id,
                    SignalrServer.NotificationUpdated,
                    new { id, notification = updated });

                return Ok(success);
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
    }
}
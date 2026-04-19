using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Feedback;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Feedbacks;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/feedbacks")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class FeedbacksController : BaseController
    {
        private readonly IFeedbackService feedbackService;

        public FeedbacksController(
            IFeedbackService feedbackService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.feedbackService = feedbackService;
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Customer,System Admin,Admin")]
        public async Task<ActionResult<FeedbackItem>> GetFeedbackById([Required] Guid id)
        {
            try
            {
                ApplicationUser? currentUser = await GetCurrentUserAsync();
                bool isAdmin = await IsCurrentUserAdminUser();

                FeedbackItem? item = await feedbackService.GetFeedbackById(id, currentUser?.MemberId, isAdmin);
                if (item == null)
                {
                    return NotFound(new { success = false, message = "Feedback not found" });
                }

                return Ok(item);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Roles = "Customer,System Admin,Admin")]
        public async Task<ActionResult<FeedbackItem>> UpdateFeedback([Required] Guid id, [FromForm] FeedbackUpdate request)
        {
            try
            {
                ApplicationUser? currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                bool isAdmin = await IsCurrentUserAdminUser();
                FeedbackItem result = await feedbackService.UpdateFeedback(id, (Guid)currentUser.MemberId, request, isAdmin);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Customer,System Admin,Admin")]
        public async Task<IActionResult> DeleteFeedback([Required] Guid id)
        {
            try
            {
                ApplicationUser? currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                bool isAdmin = await IsCurrentUserAdminUser();
                await feedbackService.DeleteFeedback(id, (Guid)currentUser.MemberId, isAdmin);
                return Ok();
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("/api/admin/feedbacks")]
        [Authorize(Roles = "System Admin,Admin")]
        public async Task<ActionResult> GetAdminFeedbacks([FromQuery] FeedbackFilterParams filter)
        {
            try
            {
                return Ok(await feedbackService.GetAdminFeedbacks(filter));
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}
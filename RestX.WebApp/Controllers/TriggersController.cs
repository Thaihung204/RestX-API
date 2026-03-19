using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestX.BLL.DataTransferObjects.Triggers;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;

namespace RestX.App.Controllers.Api
{
    //[Authorize(Roles = UtilitiesHelper.AdminRoles)]

    [ApiController]
    //[Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/triggers")]
    public class TriggersController : BaseController
    {
        private readonly ITriggerService triggerService;

        public TriggersController(
            ITriggerService triggerService,
            ILoggerFactory loggerFactory,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant)
            : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.triggerService = triggerService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAllTriggers()
        {
            try
            {
                return Ok(await this.triggerService.GetTriggers());
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrigger(Guid id)
        {
            try
            {
                return Ok(await this.triggerService.GetTriggerById(id));
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTrigger(Guid id, [FromBody] Trigger data)
        {
            try
            {
                await this.triggerService.UpsertTrigger(data, (await GetCurrentUserAsync())?.Id.ToString());
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

        [HttpPost("")]
        public async Task<IActionResult> InsertTrigger([FromBody] Trigger data)
        {
            try
            {
                await this.triggerService.UpsertTrigger(data, (await GetCurrentUserAsync())?.Id.ToString());
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

        [HttpDelete("{triggerId}")]
        public async Task<IActionResult> DeleteTrigger(Guid triggerId)
        {
            try
            {
                await this.triggerService.DeleteTrigger(triggerId, (await GetCurrentUserAsync())?.Id.ToString());
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

        [HttpPatch("{triggerId}/status/{isActive}")]
        public async Task<IActionResult> SetTriggerStatus(Guid triggerId, bool isActive)
        {
            try
            {
                await this.triggerService.SetTriggerStatus(triggerId, isActive, (await this.GetCurrentUserAsync())?.Id.ToString());
                return this.Ok();
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


        [HttpGet("objects")]
        public async Task<IActionResult> GetObjects()
        {
            try
            {
                return Ok(await this.triggerService.GetTriggerObjects());
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

        [HttpGet("objects/{objectId}/properties")]
        public async Task<IActionResult> GetObjectProperties(int objectId)
        {
            try
            {
                return Ok(await this.triggerService.GetTriggerObjectProperties(objectId));
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


        [HttpGet("types")]
        public async Task<IActionResult> GetTriggerTypes()
        {
            try
            {
                return await Task.FromResult(Ok(this.triggerService.GetTriggerTypes()));
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

        [HttpGet("criteria/types")]
        public async Task<IActionResult> GetTriggerCriteriaTypes()
        {
            try
            {
                return await Task.FromResult(Ok(this.triggerService.GetTriggerCriteriaTypes()));
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

        [HttpGet("actions/types")]
        public async Task<IActionResult> GetTriggerActionTypes()
        {
            try
            {
                return Ok(await this.triggerService.GetTriggerActionTypes());
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
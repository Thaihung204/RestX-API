using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Floor;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;
using RestX.WebApp.Helpers;

namespace RestX.WebApp.Controllers
{
    [Route("api/floors")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class FloorsController : BaseController
    {
        private readonly IFloorService floorService;

        public FloorsController(
            IFloorService floorService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.floorService = floorService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllFloors()
        {
            try
            {
                var floors = await floorService.GetAllFloors();
                return Ok(new { success = true, data = floors });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to get floors" });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFloorById([Required] Guid id)
        {
            try
            {
                var floor = await floorService.GetFloorById(id);
                if (floor == null)
                    return NotFound(new { success = false, message = "Floor not found" });
                return Ok(new { success = true, data = floor });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to get floor" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> AddFloor([FromForm] Floor request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                var user = await GetCurrentUserAsync();
                var floorId = await floorService.UpsertFloor(request, user?.Id.ToString());
                return Ok(new { success = true, message = "Floor created successfully", data = new { id = floorId } });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to create floor" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> EditFloor([Required] Guid id, [FromForm] Floor request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                request.Id = id;
                var user = await GetCurrentUserAsync();
                var floorId = await floorService.UpsertFloor(request, user?.Id.ToString());
                return Ok(new { success = true, message = "Floor updated successfully", data = new { id = floorId } });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (InvalidOperationException)
            {
                return NotFound(new { success = false, message = "Floor not found" });
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to update floor" });
            }
        }

        [HttpGet("{floorId}/layout")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFloorLayout([Required] Guid floorId, [FromQuery] DateTime? at = null)
        {
            try
            {
                var layout = await floorService.GetFloorLayout(floorId, at);
                if (layout == null)
                    return NotFound(new { success = false, message = "Floor layout not found" });
                return Ok(new { success = true, data = layout });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to get floor layout" });
            }
        }

        [HttpPut("{floorId}/layout")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> SaveLayout([Required] Guid floorId, [FromBody] SaveLayoutRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                var user = await GetCurrentUserAsync();
                var success = await floorService.SaveLayout(floorId, request, user?.Id.ToString());
                if (!success)
                    return NotFound(new { success = false, message = "Floor layout not found" });

                await BroadcastToTenant(SignalrServer.TableLayoutUpdated, new { floorId });
                return Ok(new { success = true, message = "Layout saved successfully" });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to save layout" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteFloor([Required] Guid id)
        {
            try
            {
                var success = await floorService.DeleteFloor(id);
                if (!success)
                    return NotFound(new { success = false, message = "Floor not found" });
                return Ok(new { success = true, message = "Floor deleted successfully" });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "Failed to delete floor" });
            }
        }
    }
}

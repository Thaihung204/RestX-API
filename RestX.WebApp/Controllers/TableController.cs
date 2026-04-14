using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Enum;
using RestX.Models.Identity;
using RestX.Models.Reservations;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using RestX.WebApp.Helpers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/tables")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class TablesController : BaseController
    {
        private readonly ITableService tableService;
        private readonly IHubContext<SignalrServer> hub;

        public TablesController(
            ITableService tableService,
            IHubContext<SignalrServer> hub,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.tableService = tableService;
            this.hub = hub;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TableItem>>> GetAllTables()
        {
            try
            {
                return Ok(await tableService.GetAllTables());
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

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<TableItem>> GetTableById([Required] Guid id)
        {
            try
            {
                return Ok(await tableService.GetTableById(id));
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

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult<TableItem>> EditTable([Required] Guid id, [FromForm] TableItem request)
        {
            try
            {
                return Ok(await tableService.UpsertTable(id, request));
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

        [HttpPost]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<ActionResult<TableItem>> AddTable([FromBody] TableItem request)
        {
            try
            {
                return Ok(await tableService.UpsertTable(null, request));
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

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteTable([Required] Guid id)
        {
            try
            {
                await tableService.DeleteTable(id);
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

        [HttpPut("{id}/status")]
        public async Task<ActionResult<TableItem>> ChangeStatus([Required] Guid id, [FromBody] TableStatus status)
        {
            try
            {
                var result = await tableService.ChangeTableStatus(id, status);
                await hub.BroadcastToTenant(CurrentTenant.Id, SignalrServer.TableStatusChanged, new
                {
                    tableId = result.Id,
                    tableCode = result.Code,
                    floorId = result.FloorId,
                    status = (int)result.TableStatusId,
                    statusName = result.TableStatusId.ToString()
                });
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

        [HttpGet("sessions")]
        [Authorize(Roles = "System Admin,Admin,Staff")]
        public async Task<ActionResult<IEnumerable<TableSessionInfo>>> GetAllTableSession([FromQuery] DateTime? at = null)
        {
            try
            {
                return Ok(await tableService.GetAllTableSession(at));
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

        [HttpPost("{tableId:guid}/sessions")]
        [Authorize(Roles = "System Admin,Admin,Staff,Customer")]
        public async Task<ActionResult<TableSession>> CreateTableSession(
            [Required] Guid tableId,
            [FromQuery] Guid? customerId = null,
            [FromQuery] Guid? reservationId = null)
        {
            try
            {
                ApplicationUser? currentUser = await GetCurrentUserAsync();
                string userId = string.Empty;
                if (currentUser?.Id != null)
                {
                    userId = currentUser.MemberId.ToString();
                }

                TableSession result = await tableService.CreateTableSession(tableId, userId, customerId, reservationId);

                await hub.BroadcastToTenant(CurrentTenant.Id, SignalrServer.TableSessionCreated, new
                {
                    sessionId = result.Id,
                    tableId = result.TableId,
                    orderId = result.OrderId,
                    reservationId = result.ReservationId,
                    startedAt = result.StartedAt,
                    endedAt = result.EndedAt,
                    isActive = result.IsActive
                });

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

        [HttpPut("orders/{orderId:guid}/sessions/close")]
        [Authorize(Roles = "System Admin,Admin,Staff")]
        public async Task<ActionResult<object>> CloseTableSession([Required] Guid orderId)
        {
            try
            {
                int closedCount = await tableService.CloseTableSession(orderId);

                await hub.BroadcastToTenant(CurrentTenant.Id, SignalrServer.TableSessionClosed, new
                {
                    orderId,
                    closedSessions = closedCount,
                    closedAt = DateTime.UtcNow.AddHours(7)
                });

                return Ok(new { closedSessions = closedCount });
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

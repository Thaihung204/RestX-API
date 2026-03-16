using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Enum;
using RestX.Models.Identity;
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

        public TablesController(
            ITableService tableService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.tableService = tableService;
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
        public async Task<ActionResult<TableItem>> EditTable([Required] Guid id, [FromBody] TableItem request)
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
                await BroadcastToTenant(SignalrServer.TableStatusChanged, new
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

    }
}

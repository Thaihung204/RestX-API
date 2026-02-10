using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.Models.Enum;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/tables")]
    [ApiController]
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
        public async Task<ActionResult<IEnumerable<TableItem>>> GetAllTables()
        {
            try
            {
                return Ok(await tableService.GetAllTables());
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TableItem>> GetTableById([Required] Guid id)
        {
            try
            {
                return Ok(await tableService.GetTableById(id));
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TableItem>> EditTable([Required] Guid id, [FromBody] TableItem request)
        {
            try
            {
                return Ok(await tableService.UpsertTable(id, request));
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<TableItem>> AddTable([FromBody] TableItem request)
        {
            try
            {
                return Ok(await tableService.UpsertTable(null, request));
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTable([Required] Guid id)
        {
            try
            {
                await tableService.DeleteTable(id);
                return Ok();
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
                return Ok(await tableService.ChangeTableStatus(id, status));
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}

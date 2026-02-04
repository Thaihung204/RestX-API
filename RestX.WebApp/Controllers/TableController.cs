using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
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
            IExceptionHandler exceptionHandler
        ) : base(exceptionHandler)
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
                exceptionHandler.RaiseException(ex);
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
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TableItem>> EditTable([Required] Guid id, [FromBody] TableRequest request)
        {
            try
            {
                return Ok(await tableService.UpsertTable(id, request));
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<TableItem>> AddTable([FromBody] TableRequest request)
        {
            try
            {
                return Ok(await tableService.UpsertTable(null, request));
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
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
                exceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}

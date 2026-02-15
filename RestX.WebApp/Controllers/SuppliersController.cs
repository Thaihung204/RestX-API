using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Inventory;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/suppliers")]
    [ApiController]
    public class SuppliersController : BaseController
    {
        private readonly ISupplierService supplierService;

        public SuppliersController(
            ISupplierService supplierService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant
        ) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.supplierService = supplierService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierItem>>> GetAllSuppliers()
        {
            try
            {
                var suppliers = await supplierService.GetAllSuppliers();
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierItem>> GetSupplierById([Required] Guid id)
        {
            try
            {
                var supplier = await supplierService.GetSupplierById(id);
                return Ok(supplier);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditSupplier([Required] Guid id, [FromBody] SupplierItem supplier)
        {
            try
            {
                supplier.Id = id;
                var supplierId = await supplierService.UpsertSupplier(supplier);
                return Ok(supplierId);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> AddSupplier([FromBody] SupplierItem supplier)
        {
            try
            {
                var supplierId = await supplierService.UpsertSupplier(supplier);
                return Ok(supplierId);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier([Required] Guid id)
        {
            try
            {
                await supplierService.DeleteSupplier(id);
                return Ok();
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest("An internal error occurred");
            }
        }
    }
}
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace RestX.WebApp.Controllers
{
    [Route("api/customers")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CustomerController : BaseController
    {
        private readonly ICustomerService customerService;
        private readonly ICsvExportService csvExportService;
        public CustomerController(ICustomerService customerService,
            ICsvExportService csvExportService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.customerService = customerService;
            this.csvExportService = csvExportService;
        }
        [HttpGet]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> GetAllCustomers([FromQuery] CustomerFilterParams filter)
        {
            try
            {
                var result = await customerService.GetAllCustomers(filter);
                return Ok(new { success = true, data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,System Admin,Customer")]
        public async Task<IActionResult> GetCustomerById([Required] Guid id)
        {
            try
            {
                var customer = await customerService.GetCustomerById(id);
                if (customer == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }
                return Ok(new { success = true, data = customer });
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
        [HttpPost]
        [Authorize(Roles = "Admin,System Admin,Customer")]
        public async Task<IActionResult> CreateCustomer([FromForm] CreateCustomer dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }
                var result = await customerService.CreateCustomer(dto);
                return Ok(new { success = true, message = "Customer created successfully", data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,System Admin,Customer")]
        public async Task<IActionResult> UpdateCustomer([Required] Guid id, [FromForm] UpdateCustomer dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }
                var result = await customerService.UpdateCustomer(id, dto);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }
                return Ok(new { success = true, message = "Customer updated successfully", data = result });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
        [HttpGet("export/csv")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> ExportCustomersCsv([FromQuery] CustomerFilterParams filter)
        {
            try
            {
                var bytes = await csvExportService.ExportCustomersCsvAsync(filter);
                var fileName = $"customers_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmmss}.csv";
                return File(bytes, MediaTypeNames.Text.Csv, fileName);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,System Admin")]
        public async Task<IActionResult> DeleteCustomer([Required] Guid id)
        {
            try
            {
                var success = await customerService.DeleteCustomer(id);
                if (!success)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }
                return Ok(new { success = true, message = "Customer deleted successfully" });
            }
            catch (AppException ex)
            {
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.Models.Customers;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : BaseController
    {
        private readonly ICustomerService customerService;
        public CustomerController(ICustomerService customerService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            this.customerService = customerService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetAllCustomers()
        {
            try
            {
                var customers = await customerService.GetAllCustomers();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomerById([Required] Guid id)
        {
            try
            {
                var customer = await customerService.GetCustomerById(id);
                if (customer == null) return NotFound();
                return Ok(customer);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditCustomer([Required] Guid id, [FromBody] Customer customer)
        {
            try
            {
                customer.Id = id;
                var result = await customerService.UpsertCustomer(customer);
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> AddCustomer([FromBody] Customer customer)
        {
            try
            {
                var created = await customerService.UpsertCustomer(customer);
                return Ok(created);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer([Required] Guid id)
        {
            try
            {
                await customerService.DeleteCustomer(id);
                return Ok();
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DTOs.Customer;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/customers")]
    [ApiController]
    public class CustomerController : BaseController
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            _customerService = customerService;
        }


        [HttpGet]
        public async Task<IActionResult> GetAllCustomers([FromQuery] CustomerFilterParams filter)
        {
            try
            {
                var result = await _customerService.GetAllCustomers(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById([Required] Guid id)
        {
            try
            {
                var customer = await _customerService.GetCustomerById(id);
                if (customer == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }
                return Ok(new { success = true, data = customer });
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var result = await _customerService.CreateCustomer(dto);
                return Ok(new { success = true, message = "Customer created successfully", data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer([Required] Guid id, [FromBody] UpdateCustomerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var result = await _customerService.UpdateCustomer(id, dto);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }
                return Ok(new { success = true, message = "Customer updated successfully", data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer([Required] Guid id)
        {
            try
            {
                var success = await _customerService.DeleteCustomer(id);
                if (!success)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }
                return Ok(new { success = true, message = "Customer deleted successfully" });
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
    }
}

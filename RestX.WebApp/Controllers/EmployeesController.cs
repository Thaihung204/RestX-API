using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Employee;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Employees;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/employees")]
    [ApiController]
    public class EmployeesController : BaseController
    {
        private readonly IEmployeeService employeeService;
        public EmployeesController(IEmployeeService employeeService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            this.employeeService = employeeService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllEmployees([FromQuery] EmployeeFilterParams filter)
        {
            try
            {
                var result = await employeeService.GetAllEmployeesPaginated(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById([Required] Guid id)
        {
            try
            {
                var employee = await employeeService.GetEmployeeById(id);
                if (employee == null)
                {
                    return NotFound(new { success = false, message = "Employee not found" });
                }
                return Ok(new { success = true, data = employee });
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromForm] CreateEmployee dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }
                var result = await employeeService.CreateEmployee(dto);
                return Ok(new { success = true, message = "Employee created successfully", data = result });
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
        public async Task<IActionResult> UpdateEmployee([Required] Guid id, [FromForm] UpdateEmployee dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }
                var result = await employeeService.UpdateEmployee(id, dto);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Employee not found" });
                }
                return Ok(new { success = true, message = "Employee updated successfully", data = result });
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
        public async Task<IActionResult> DeleteEmployee([Required] Guid id)
        {
            try
            {
                var success = await employeeService.DeleteEmployee(id);
                if (!success)
                {
                    return NotFound(new { success = false, message = "Employee not found" });
                }
                return Ok(new { success = true, message = "Employee deleted successfully" });
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(new { success = false, message = "An internal error occurred" });
            }
        }
    }
}

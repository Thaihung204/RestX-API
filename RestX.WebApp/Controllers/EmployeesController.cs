using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Employees;
using RestX.Models.HR;
using RestX.WebApp.Controllers.BaseControllers;
using System.ComponentModel.DataAnnotations;

namespace RestX.WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : BaseController
    {
        private readonly IEmployeeService employeeService;

        public EmployeesController(IEmployeeService employeeService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            this.employeeService = employeeService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAllEmployees()
        {
            try
            {
                var employees = await employeeService.GetAllEmployees();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployeeById([Required] Guid id)
        {
            try
            {
                var employee = await employeeService.GetEmployeeById(id);
                if (employee == null) return NotFound();
                return Ok(employee);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditEmployee([Required] Guid id, [FromBody] Employee employee)
        {
            try
            {
                employee.Id = id;
                var result = await employeeService.UpsertEmployee(employee);
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> AddEmployee([FromBody] Employee employee)
        {
            try
            {
                var created = await employeeService.UpsertEmployee(employee);
                return Ok(created);
            }
            catch (Exception ex)
            {
                this.exceptionHandler.RaiseException(ex);
                return this.BadRequest("An internal error occurred");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee([Required] Guid id)
        {
            try
            {
                await employeeService.DeleteEmployee(id);
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

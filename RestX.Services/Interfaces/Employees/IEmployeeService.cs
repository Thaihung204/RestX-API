using RestX.Models.HR;

namespace RestX.BLL.Interfaces.Employees
{
    public interface IEmployeeService
    {
        Task<IEnumerable<Employee>> GetAllEmployees();
        Task<Employee> GetEmployeeById(Guid id);
        Task<Employee> UpsertEmployee(Employee model);
        Task DeleteEmployee(Guid id);
    }
}
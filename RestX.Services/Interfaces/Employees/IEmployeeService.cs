using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Employee;
namespace RestX.BLL.Interfaces.Employees
{
    public interface IEmployeeService
    {
        Task<PaginatedResult<EmployeeListItem>> GetAllEmployeesPaginated(EmployeeFilterParams filter);
        Task<EmployeeResponse?> GetEmployeeById(Guid id);
        Task<EmployeeResponse> CreateEmployee(CreateEmployee dto);
        Task<EmployeeResponse?> UpdateEmployee(Guid id, UpdateEmployee dto);
        Task<bool> DeleteEmployee(Guid id);
    }
}

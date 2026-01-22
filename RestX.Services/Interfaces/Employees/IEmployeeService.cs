using RestX.BLL.DTOs.Common;
using RestX.BLL.DTOs.Employee;

namespace RestX.BLL.Interfaces.Employees
{
    public interface IEmployeeService
    {
        Task<PaginatedResult<EmployeeListItemDto>> GetAllEmployeesPaginated(EmployeeFilterParams filter);
        Task<EmployeeResponseDto?> GetEmployeeById(Guid id);
        Task<EmployeeResponseDto> CreateEmployee(CreateEmployeeDto dto);
        Task<EmployeeResponseDto?> UpdateEmployee(Guid id, UpdateEmployeeDto dto);
        Task<bool> DeleteEmployee(Guid id);
    }
}

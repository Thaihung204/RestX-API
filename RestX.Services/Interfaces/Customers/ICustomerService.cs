using RestX.BLL.DTOs.Common;
using RestX.BLL.DTOs.Customer;

namespace RestX.BLL.Interfaces.Customers
{
    public interface ICustomerService
    {
        Task<PaginatedResult<CustomerListItemDto>> GetAllCustomers(CustomerFilterParams filter);
        Task<CustomerResponseDto?> GetCustomerById(Guid id);
        Task<CustomerResponseDto> CreateCustomer(CreateCustomerDto dto);
        Task<CustomerResponseDto?> UpdateCustomer(Guid id, UpdateCustomerDto dto);
        Task<bool> DeleteCustomer(Guid id);
    }
}

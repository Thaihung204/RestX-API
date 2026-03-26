using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.Models.Customers;

namespace RestX.BLL.Interfaces.Customers
{
    public interface ICustomerService
    {
        Task<PaginatedResult<CustomerListItem>> GetAllCustomers(CustomerFilterParams filter);
        Task<CustomerResponse?> GetCustomerById(Guid id);
        Task<CustomerResponse> CreateCustomer(CreateCustomer dto);
        Task<CustomerResponse?> UpdateCustomer(Guid id, UpdateCustomer dto);
        Task<bool> DeleteCustomer(Guid id);
        Task<Guid?> GetCustomerIdByApplicationUserIdAsync(Guid applicationUserId);
        Task<Customer> CreateCustomerRecord(Guid userId);
    }
}

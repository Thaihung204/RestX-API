using RestX.Models.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Interfaces.Customers
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAllCustomers();
        Task<Customer> GetCustomerById(Guid id);
        Task<Customer> UpsertCustomer(Customer model);
        Task DeleteCustomer(Guid id);
    }
}

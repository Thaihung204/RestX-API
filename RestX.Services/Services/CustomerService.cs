using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.Models.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Services
{
    public class CustomerService : BaseService, ICustomerService
    {
        private readonly IRepository _repo;
        public CustomerService(IRepository repo) : base(repo)
        {
            this._repo = repo;
        }

        public async Task DeleteCustomer(Guid id)
        {
            _repo.Delete<Customer>(id);
            await _repo.SaveAsync();
        }

        public async Task<IEnumerable<Customer>> GetAllCustomers()
        {
            var customers = await _repo.GetAllAsync<Customer>();
            return customers.ToList();
        }

        public async Task<Customer> GetCustomerById(Guid id)
        {
            return await _repo.GetByIdAsync<Customer>(id);
        }

        public async Task<Customer> UpsertCustomer(Customer model)
        {
            if (model.Id != Guid.Empty)
            {
                var existing = await _repo.GetByIdAsync<Customer>(model.Id);
                if (existing != null)
                {
                    existing.MembershipLevel = model.MembershipLevel;
                    existing.LoyaltyPoints = model.LoyaltyPoints;
                    existing.IsActive = model.IsActive;

                    _repo.Update(existing);
                    await _repo.SaveAsync();
                    return existing;
                }
            }

            if (model.Id == Guid.Empty)
            {
                model.Id = Guid.NewGuid();
            }

            await _repo.CreateAsync(model);
            return model;
        }
    }
}

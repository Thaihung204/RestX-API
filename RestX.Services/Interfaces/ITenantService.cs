using RestX.Models.Tenants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Interfaces
{
    public interface ITenantService
    {
        Task<List<Tenant>> GetTenantsAsync();
        Task<Tenant> GetTenantByIdAsync(int id);
        Task<Tenant> UpsertTenantAsync(Tenant tenant);
        Task DeleteTenantAsync(int id);
    }
}

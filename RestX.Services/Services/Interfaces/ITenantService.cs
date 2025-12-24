using RestX.Models.Tenants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Services.Interfaces
{
    public interface ITenantService
    {
        Task<List<Tenant>> GetTenantsAsync();
        Task<Tenant> GetTenantByIdAsync(Guid id);
        Task<Tenant> UpsertTenantAsync(Tenant tenant);
        Task DeleteTenantAsync(Guid id);
    }
}
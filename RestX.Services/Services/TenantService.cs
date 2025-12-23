using RestX.BLL.Interfaces;
using RestX.BLL.Services.Interfaces;
using RestX.BLL.Services.Services;
using RestX.Models.Tenants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Services
{
    public class TenantService : BaseService, ITenantService
    {
        public TenantService(IRepository repo) : base(repo)
        {
        }

        public async Task DeleteTenantAsync(int id)
        {
            var tenant = await GetTenantByIdAsync(id);
            Repo.Delete<Tenant>(id);
            await Repo.SaveAsync();
        }

        public async Task<Tenant> GetTenantByIdAsync(int id)
        {
            var tenant= await Repo.GetByIdAsync<Tenant>(id);
            return tenant;
        }

        public async Task<List<Tenant>> GetTenantsAsync()
        {
            var tenants = await Repo.GetAllAsync<Tenant>();
            return tenants.ToList();
        }

        public async Task<Tenant> UpsertTenantAsync(Tenant tenant)
        {
            if (tenant.Id == 0)
            {
                await Repo.CreateAsync(tenant);
            }
            else
            {
                var existingTenant = await Repo.GetByIdAsync<Tenant>(tenant.Id);
                if (existingTenant == null)
                {
                    await Repo.CreateAsync(tenant);
                }
                else
                {
                    Repo.Update(tenant);
                }
            }

            await Repo.SaveAsync();
            return tenant;
        }
    }
}

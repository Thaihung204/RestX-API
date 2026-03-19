using RestX.BLL.Services;
using RestX.DAL.Context;
using RestX.Models.Tenants;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestX.BLL.Interfaces
{
    public interface IRepoHelper
    {
        EntityFrameworkRepository<TenantDbContext> GetTenantRepo(ActiveTenant activeTenant);
        Task<ActiveTenant> GetAllActiveTenantsObject(Guid? tenantId = null);
    }
}

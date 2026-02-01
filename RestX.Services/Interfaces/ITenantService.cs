using RestX.BLL.DataTranferObjects.Tenants;
using RestX.Models.Tenants;

namespace RestX.BLL.Interfaces
{
    public interface ITenantService
    {
        Task<IEnumerable<Tenant>> GetAllTenants();
        Task<TenantOverview> GetTenantByIdOrHostname(string id);
        Task<Tenant> UpsertTenant(Tenant model);
        Task DeleteTenant(string id);
    }
}
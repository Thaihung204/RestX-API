using RestX.Models.Tenants;

namespace RestX.BLL.Interfaces
{
    public interface ITenantService
    {
        Task<IEnumerable<Tenant>> GetAllTenants();
        Task<Tenant> GetTenantByIdOrHostname(string id);
        Task<Tenant> UpsertTenant(Tenant model);
        Task DeleteTenant(string id);
    }
}
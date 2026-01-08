using RestX.Models.Tenants;

namespace RestX.BLL.Interfaces
{
    public interface ITenantService
    {
        Task<IEnumerable<Tenant>> GetAllTenants();
        Task<Tenant> GetTenantById(Guid id);
        Task<Tenant> UpsertTenant(Tenant model);
        Task DeleteTenant(Guid id);
    }
}
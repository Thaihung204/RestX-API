using RestX.Models.Tenants;

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
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.Models.Tenants;

namespace RestX.BLL.Interfaces
{
    public interface ITenantService
    {
        Task<IEnumerable<Tenant>> GetAllTenants();
        Task<TenantOverview> GetTenantByIdOrHostname(string id);
        Task<Tenant> UpsertTenant(TenantItem model);
        Task DeleteTenant(string id);
        Task<IEnumerable<DataTranferObjects.Tenants.TenantRequest>> GetAllTenantRequests();
        Task<DataTranferObjects.Tenants.TenantRequest?> GetTenantRequestById(Guid tenantRequestsId);
        Task<Guid> AddTenantRequest(DataTranferObjects.Tenants.TenantRequest tenantRequest);
        Task<Guid> AcceptTenantRequest(Guid tenantRequestsId);
        Task<Guid> DeclineTenantRequest(Guid tenantRequestsId);
        Task DeleteTenantRequest(Guid tenantRequestsId);
    }
}
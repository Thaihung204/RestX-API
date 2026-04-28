using Microsoft.AspNetCore.Http;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.Models.Tenants;

namespace RestX.BLL.Interfaces
{
    public interface ITenantService
    {
        Task<IEnumerable<BusinessHourDto>> GetBusinessHours(Guid tenantId);
        Task UpdateBusinessHours(Guid tenantId, IEnumerable<BusinessHourDto> hours);
        Task<IEnumerable<Tenant>> GetAllTenants();
        Task<TenantOverview> GetTenantByIdOrHostname(string id);
        Task<Tenant> UpdateTenant(TenantItem model);
        Task<string> UploadAndCreateTenant(TenantItem model);
        Task CreateTenant(TenantItem model);
        Task DeleteTenant(string id);
        Task ChangeTenantStatus(Guid id, bool status);
        Task<IEnumerable<DataTranferObjects.Tenants.TenantRequest>> GetAllTenantRequests();
        Task<DataTranferObjects.Tenants.TenantRequest?> GetTenantRequestById(Guid tenantRequestsId);
        Task<Guid> AddTenantRequest(DataTranferObjects.Tenants.TenantRequest tenantRequest);
        Task<string> AcceptTenantRequest(Guid tenantRequestsId); Task<Guid> DeclineTenantRequest(Guid tenantRequestsId);
        Task DeleteTenantRequest(Guid tenantRequestsId);
    }
}
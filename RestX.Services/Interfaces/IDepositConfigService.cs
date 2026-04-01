using RestX.BLL.DataTranferObjects.Tenants;

namespace RestX.BLL.Interfaces
{
    public interface IDepositConfigService
    {
        Task<DepositConfig?> GetDepositConfig(Guid tenantId);
        Task UpsertDepositConfig(Guid tenantId, DepositConfig config);
        Task DeleteDepositConfig(Guid tenantId);
    }
}

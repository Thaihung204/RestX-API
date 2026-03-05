using RestX.BLL.DataTranferObjects.Common;

namespace RestX.BLL.Interfaces
{
    public interface IPaymentSettingService
    {
        Task<PaymentGatewaySettings?> GetPaymentSettingByTenantId(Guid tenantId);
        Task UpsertPaymentSetting(Guid tenantId, PaymentGatewaySettings settings);
    }
}

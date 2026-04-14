using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestX.AdminDAL.Context;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.Extensions;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class PaymentSettingService : IPaymentSettingService
    {
        private readonly RestxAdminContext adminContext;
        private readonly IRedisService redisService;

        public PaymentSettingService(RestxAdminContext adminContext, IRedisService redisService)
        {
            this.adminContext = adminContext;
            this.redisService = redisService;
        }

        private static string CacheKey(Guid tenantId) => $"payment-settings:{tenantId}";

        public async Task<PaymentGatewaySettings?> GetPaymentSettingByTenantId(Guid tenantId)
        {
            var cached = await redisService.GetAsync<PaymentGatewaySettings>(CacheKey(tenantId));
            if (cached != null) return cached;

            var setting = await adminContext.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Key.ToLower() == PaymentConstants.SettingKey.Payment);

            if (setting == null) return null;

            var result = JsonConvert.DeserializeObject<PaymentGatewaySettings>(setting.Value);
            if (result != null)
                await redisService.SetAsync(CacheKey(tenantId), result, TimeSpan.FromMinutes(30));

            return result;
        }

        public async Task UpsertPaymentSetting(Guid tenantId, PaymentGatewaySettings settings)
        {
            var existing = await adminContext.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Key.ToLower() == PaymentConstants.SettingKey.Payment);

            PaymentGatewaySettings? oldUrl = null;
            string hostname = string.Empty;
            if (existing != null)
            {
                oldUrl = JsonConvert.DeserializeObject<PaymentGatewaySettings>(existing.Value);
                hostname = oldUrl?.ReturnUrl?.Split('/').Length > 2
                    ? oldUrl.ReturnUrl.Split('/')[2]
                    : string.Empty;
            }
            if (string.IsNullOrEmpty(hostname))
            {
                var tenant = await adminContext.Tenants.FindAsync(tenantId);
                hostname = tenant?.Hostname ?? string.Empty;
            }

            settings.ReturnUrl = !string.IsNullOrEmpty(settings.ReturnUrl) ? settings.ReturnUrl : (oldUrl?.ReturnUrl ?? $"{hostname}/payment/success");
            settings.CancelUrl = !string.IsNullOrEmpty(settings.CancelUrl) ? settings.CancelUrl : (oldUrl?.CancelUrl ?? $"{hostname}/payment/cancel");
            settings.ReturnDepositUrl = !string.IsNullOrEmpty(settings.ReturnDepositUrl) ? settings.ReturnDepositUrl : (oldUrl?.ReturnDepositUrl ?? $"{hostname}/payment/deposit/success");
            settings.CancelDepositUrl = !string.IsNullOrEmpty(settings.CancelDepositUrl) ? settings.CancelDepositUrl : (oldUrl?.CancelDepositUrl ?? $"{hostname}/payment/deposit/cancel");

            var json = JsonConvert.SerializeObject(settings);

            if (existing != null)
                existing.Value = json;
            else
                adminContext.TenantSettings.Add(new TenantSetting { TenantId = tenantId, Key = PaymentConstants.SettingKey.Payment, Value = json });

            await adminContext.SaveChangesAsync();
            await redisService.RemoveAsync(CacheKey(tenantId));
        }
    }
}

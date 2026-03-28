using RestX.AdminDAL.Context;
using RestX.BLL.Interfaces;
using RestX.Models.Admin;
using AdminDepositConfig = RestX.Models.Admin.DepositConfig;
using DepositConfigDto = RestX.BLL.DataTranferObjects.Tenants.DepositConfig;

namespace RestX.BLL.Services
{
    public class DepositConfigService : IDepositConfigService
    {
        private readonly RestxAdminContext adminContext;

        public DepositConfigService(RestxAdminContext adminContext)
        {
            this.adminContext = adminContext;
        }

        public async Task<DepositConfigDto?> GetDepositConfig(Guid tenantId)
        {
            var config = await adminContext.DepositConfigs.FindAsync(tenantId);
            if (config == null) return null;

            return new DepositConfigDto
            {
                MinPartySize = config.MinPartySize,
                DepositAmountPerPerson = config.DepositAmountPerPerson,
                DeadlineHours = config.DeadlineHours,
                EarlyRefundHours = config.EarlyRefundHours,
                EarlyRefundPercentage = config.EarlyRefundPercentage,
                LateRefundHours = config.LateRefundHours,
                LateRefundPercentage = config.LateRefundPercentage
            };
        }

        public async Task UpsertDepositConfig(Guid tenantId, DepositConfigDto dto)
        {
            var existing = await adminContext.DepositConfigs.FindAsync(tenantId);
            if (existing == null)
            {
                adminContext.DepositConfigs.Add(new AdminDepositConfig
                {
                    TenantId = tenantId,
                    MinPartySize = dto.MinPartySize,
                    DepositAmountPerPerson = dto.DepositAmountPerPerson,
                    DeadlineHours = dto.DeadlineHours,
                    EarlyRefundHours = dto.EarlyRefundHours,
                    EarlyRefundPercentage = dto.EarlyRefundPercentage,
                    LateRefundHours = dto.LateRefundHours,
                    LateRefundPercentage = dto.LateRefundPercentage
                });
            }
            else
            {
                existing.MinPartySize = dto.MinPartySize;
                existing.DepositAmountPerPerson = dto.DepositAmountPerPerson;
                existing.DeadlineHours = dto.DeadlineHours;
                existing.EarlyRefundHours = dto.EarlyRefundHours;
                existing.EarlyRefundPercentage = dto.EarlyRefundPercentage;
                existing.LateRefundHours = dto.LateRefundHours;
                existing.LateRefundPercentage = dto.LateRefundPercentage;
                adminContext.DepositConfigs.Update(existing);
            }

            await adminContext.SaveChangesAsync();
        }

        public async Task DeleteDepositConfig(Guid tenantId)
        {
            var config = await adminContext.DepositConfigs.FindAsync(tenantId);
            if (config == null)
                throw new KeyNotFoundException("Deposit config not found");

            adminContext.DepositConfigs.Remove(config);
            await adminContext.SaveChangesAsync();
        }
    }
}

using RestX.BLL.DataTranferObjects.Admin;

namespace RestX.BLL.Interfaces;

public interface IAdminSnapshotService
{
    Task TakeDailySnapshotAsync(DateOnly? forDate = null);
    Task SeedMonthSnapshotsAsync(DateOnly month);
    Task TakeMonthlySnapshotAsync(DateOnly? forMonth = null);
    Task<List<TenantSnapshotDto>> GetSnapshotsAsync(string periodType, DateOnly? fromDate = null, DateOnly? toDate = null, Guid? tenantId = null);
    Task<TenantSnapshotDetailDto> GetTenantDetailAsync(Guid tenantId, string periodType, DateOnly? fromDate = null, DateOnly? toDate = null);
}

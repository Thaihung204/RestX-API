using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestX.AdminDAL.Context;
using RestX.BLL.DataTranferObjects.Admin;
using RestX.BLL.Interfaces;
using RestX.DAL.Context;
using RestX.Models.Admin;
using RestX.Models.Enum;

namespace RestX.BLL.Services;

public class AdminSnapshotService : IAdminSnapshotService
{
    private readonly RestxAdminContext adminContext;
    private readonly IMapper mapper;

    public AdminSnapshotService(RestxAdminContext adminContext, IMapper mapper)
    {
        this.adminContext = adminContext;
        this.mapper = mapper;
    }

    public async Task TakeDailySnapshotAsync(DateOnly? forDate = null)
    {
        var yesterday = forDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(-1));
        var fromDate = yesterday.ToDateTime(TimeOnly.MinValue);
        var toDate = yesterday.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var tenants = await adminContext.Tenants
            .Where(t => t.Status)
            .ToListAsync();

        foreach (var tenant in tenants)
        {
            var snapshot = new TenantSnapshot
            {
                TenantId = tenant.Id,
                PeriodStart = yesterday,
                PeriodType = "daily",
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            try
            {
                var options = new DbContextOptionsBuilder<TenantDbContext>()
                    .UseSqlServer(tenant.ConnectionString, sql => sql.EnableRetryOnFailure(3))
                    .Options;

                using var tenantDb = new TenantDbContext(options);

                snapshot.Revenue = await GetRevenue(tenantDb, fromDate, toDate);
                snapshot.DiscountAmount = await GetDiscountAmount(tenantDb, fromDate, toDate);

                var orderStats = await GetOrderStats(tenantDb, fromDate, toDate);
                snapshot.TotalOrders = orderStats.total;
                snapshot.CompletedOrders = orderStats.completed;
                snapshot.CancelledOrders = orderStats.cancelled;

                snapshot.TotalCustomers = await tenantDb.Customers.CountAsync();
                snapshot.NewCustomers = await tenantDb.Customers
                    .CountAsync(c => c.CreatedDate >= fromDate && c.CreatedDate < toDate);

                snapshot.NewReservations = await GetNewReservations(tenantDb, fromDate, toDate);

                snapshot.IsSuccess = true;
            }
            catch (Exception ex)
            {
                snapshot.IsSuccess = false;
                snapshot.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }

            adminContext.TenantSnapshots.Add(snapshot);
        }

        await adminContext.SaveChangesAsync();
    }

    public async Task SeedMonthSnapshotsAsync(DateOnly month)
    {
        var firstDay = new DateOnly(month.Year, month.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var existingSnapshots = adminContext.TenantSnapshots
            .Where(s => s.PeriodType == "daily"
                     && s.PeriodStart >= firstDay
                     && s.PeriodStart <= lastDay);
        adminContext.TenantSnapshots.RemoveRange(existingSnapshots);
        await adminContext.SaveChangesAsync();
        var current = firstDay;
        while (current <= lastDay && current < today)
        {
            await TakeDailySnapshotAsync(current);
            current = current.AddDays(1);
        }
        await TakeMonthlySnapshotAsync(firstDay);
    }

    public async Task TakeMonthlySnapshotAsync(DateOnly? forMonth = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var firstOfLastMonth = forMonth ?? new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var firstOfThisMonth = firstOfLastMonth.AddMonths(1);

        var tenants = await adminContext.Tenants
            .Where(t => t.Status)
            .Select(t => t.Id)
            .ToListAsync();

        foreach (var tenantId in tenants)
        {
            var dailySnapshots = await adminContext.TenantSnapshots
                .Where(s => s.TenantId == tenantId
                         && s.PeriodType == "daily"
                         && s.PeriodStart >= firstOfLastMonth
                         && s.PeriodStart < firstOfThisMonth
                         && s.IsSuccess)
                .ToListAsync();

            var snapshot = new TenantSnapshot
            {
                TenantId = tenantId,
                PeriodStart = firstOfLastMonth,
                PeriodType = "monthly",
                CreatedAt = DateTime.UtcNow.AddHours(7),
                Revenue = dailySnapshots.Sum(s => s.Revenue),
                DiscountAmount = dailySnapshots.Sum(s => s.DiscountAmount),
                TotalOrders = dailySnapshots.Sum(s => s.TotalOrders),
                CompletedOrders = dailySnapshots.Sum(s => s.CompletedOrders),
                CancelledOrders = dailySnapshots.Sum(s => s.CancelledOrders),
                TotalCustomers = dailySnapshots.OrderByDescending(s => s.PeriodStart).FirstOrDefault()?.TotalCustomers ?? 0,
                NewCustomers = dailySnapshots.Sum(s => s.NewCustomers),
                NewReservations = dailySnapshots.Sum(s => s.NewReservations),
                IsSuccess = dailySnapshots.Any()
            };

            adminContext.TenantSnapshots.Add(snapshot);
        }

        await adminContext.SaveChangesAsync();
    }

    public async Task<List<TenantSnapshotDto>> GetSnapshotsAsync(string periodType, DateOnly? fromDate = null, DateOnly? toDate = null, Guid? tenantId = null)
    {
        var (queryFrom, queryTo, queryPeriodType) = ResolvePeriod(periodType, fromDate, toDate);

        var query = adminContext.TenantSnapshots
            .Where(s => s.PeriodType == queryPeriodType
                     && s.PeriodStart >= queryFrom
                     && s.PeriodStart < queryTo
                     && s.IsSuccess);

        if (tenantId.HasValue)
            query = query.Where(s => s.TenantId == tenantId.Value);

        var snapshots = await query.ToListAsync();

        return snapshots
            .GroupBy(s => s.TenantId)
            .Select(g =>
            {
                var dto = mapper.Map<TenantSnapshotDto>(g.First());
                dto.TenantId = g.Key;
                dto.PeriodStart = queryFrom;
                dto.PeriodType = periodType;
                dto.Revenue = g.Sum(s => s.Revenue);
                dto.DiscountAmount = g.Sum(s => s.DiscountAmount);
                dto.TotalOrders = g.Sum(s => s.TotalOrders);
                dto.CompletedOrders = g.Sum(s => s.CompletedOrders);
                dto.CancelledOrders = g.Sum(s => s.CancelledOrders);
                dto.TotalCustomers = g.OrderByDescending(s => s.PeriodStart).First().TotalCustomers;
                dto.NewCustomers = g.Sum(s => s.NewCustomers);
                dto.NewReservations = g.Sum(s => s.NewReservations);
                return dto;
            })
            .ToList();
    }

    public async Task<TenantSnapshotDetailDto> GetTenantDetailAsync(Guid tenantId, string periodType, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var (queryFrom, queryTo, queryPeriodType) = ResolvePeriod(periodType, fromDate, toDate);

        var summarySnapshots = await adminContext.TenantSnapshots
            .Where(s => s.TenantId == tenantId
                     && s.PeriodType == queryPeriodType
                     && s.PeriodStart >= queryFrom
                     && s.PeriodStart < queryTo
                     && s.IsSuccess)
            .ToListAsync();

        var breakdownPeriodType = periodType.ToLower() switch
        {
            "monthly" or "month" => "daily",
            _ => "monthly"
        };

        var breakdownSnapshots = await adminContext.TenantSnapshots
            .Where(s => s.TenantId == tenantId
                     && s.PeriodType == breakdownPeriodType
                     && s.PeriodStart >= queryFrom
                     && s.PeriodStart < queryTo
                     && s.IsSuccess)
            .OrderBy(s => s.PeriodStart)
            .ToListAsync();

        return new TenantSnapshotDetailDto
        {
            TenantId = tenantId,
            PeriodType = periodType,
            PeriodStart = queryFrom,
            Revenue = summarySnapshots.Sum(s => s.Revenue),
            DiscountAmount = summarySnapshots.Sum(s => s.DiscountAmount),
            TotalOrders = summarySnapshots.Sum(s => s.TotalOrders),
            CompletedOrders = summarySnapshots.Sum(s => s.CompletedOrders),
            CancelledOrders = summarySnapshots.Sum(s => s.CancelledOrders),
            TotalCustomers = summarySnapshots.OrderByDescending(s => s.PeriodStart).FirstOrDefault()?.TotalCustomers ?? 0,
            NewCustomers = summarySnapshots.Sum(s => s.NewCustomers),
            NewReservations = summarySnapshots.Sum(s => s.NewReservations),
            Breakdown = mapper.Map<List<SnapshotBreakdownItem>>(breakdownSnapshots)
        };
    }

    private static (DateOnly from, DateOnly to, string dbPeriodType) ResolvePeriod(string periodType, DateOnly? fromDate, DateOnly? toDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        return periodType.ToLower() switch
        {
            "daily" => (
                fromDate ?? today.AddDays(-1),
                (toDate ?? today.AddDays(-1)).AddDays(1),
                "daily"),

            "monthly" or "month" => (
                fromDate ?? new DateOnly(today.Year, today.Month, 1),
                (toDate ?? new DateOnly(today.Year, today.Month, 1)).AddMonths(1),
                "daily"),

            "quarterly" or "quarter" => (
                fromDate ?? GetCurrentQuarterStart(today),
                (toDate ?? GetCurrentQuarterEnd(today)).AddMonths(1),
                "monthly"),

            "yearly" or "year" => (
                fromDate ?? new DateOnly(today.Year, 1, 1),
                (toDate ?? new DateOnly(today.Year, 12, 1)).AddMonths(1),
                "monthly"),

            _ => (
                fromDate ?? today.AddDays(-1),
                (toDate ?? today.AddDays(-1)).AddDays(1),
                "daily")
        };
    }

    private static DateOnly GetCurrentQuarterStart(DateOnly date)
    {
        var startMonth = ((date.Month - 1) / 3) * 3 + 1;
        return new DateOnly(date.Year, startMonth, 1);
    }

    private static DateOnly GetCurrentQuarterEnd(DateOnly date)
    {
        var startMonth = ((date.Month - 1) / 3) * 3 + 1;
        return new DateOnly(date.Year, startMonth + 2, 1);
    }

    #region Private query helpers

    private static async Task<decimal> GetRevenue(TenantDbContext db, DateTime from, DateTime to)
    {
        return await db.Payments
            .Where(p => p.PaymentDate >= from && p.PaymentDate < to
                     && p.Status == PaymentStatus.Success
                     && p.Purpose == PaymentPurpose.Order)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
    }

    private static async Task<decimal> GetDiscountAmount(TenantDbContext db, DateTime from, DateTime to)
    {
        return await db.Orders
            .Where(o => o.CreatedDate >= from && o.CreatedDate < to)
            .SumAsync(o => (decimal?)o.DiscountAmount) ?? 0m;
    }

    private static async Task<(int total, int completed, int cancelled)> GetOrderStats(TenantDbContext db, DateTime from, DateTime to)
    {
        var stats = await db.Orders
            .Where(o => o.CreatedDate >= from && o.CreatedDate < to)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Completed = g.Count(o => o.OrderStatusId == (int)OrderStatus.Completed),
                Cancelled = g.Count(o => o.OrderStatusId == (int)OrderStatus.Cancelled)
            })
            .FirstOrDefaultAsync();

        return (stats?.Total ?? 0, stats?.Completed ?? 0, stats?.Cancelled ?? 0);
    }

    private static async Task<int> GetNewReservations(TenantDbContext db, DateTime from, DateTime to)
    {
        return await db.Reservations
            .CountAsync(r => r.CreatedDate >= from && r.CreatedDate < to);
    }

    #endregion
}

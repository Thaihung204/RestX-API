using QuestPDF.Fluent;
using RestX.BLL.DataTranferObjects.Dashboard;
using RestX.BLL.DataTranferObjects.Reports;
using RestX.BLL.Interfaces;
using RestX.BLL.Services.Reports;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class ReportService : BaseService, IReportService
    {
        private readonly IDashboardService _dashboardService;

        public ReportService(
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant,
            IDashboardService dashboardService)
            : base(repo, redisService, tenant)
        {
            _dashboardService = dashboardService;
        }

        public async Task<ReportData> PrepareDataAsync(ReportRequest request)
        {
            var dashRequest = MapToDashboardRequest(request);
            var (fromDate, toDate) = ResolveDateRange(request);

            var summary = await _dashboardService.GetSummaryAsync(dashRequest);
            var revenueTrend = await _dashboardService.GetRevenueTrendAsync(dashRequest);
            var orderTrend = await _dashboardService.GetOrderTrendAsync(dashRequest);
            var topDishes = await _dashboardService.GetTopDishesAsync(dashRequest, request.TopDishesCount, "revenue");
            var customerStats = await _dashboardService.GetCustomerStatsAsync(dashRequest);
            var promotions = await _dashboardService.GetPromotionStatsAsync(dashRequest);

            var tenant = CurrentTenant;
            return new ReportData
            {
                ReportType = request.ReportType.ToLower(),
                PeriodLabel = BuildPeriodLabel(request.ReportType, fromDate, toDate),
                FromDate = fromDate,
                ToDate = toDate,
                GeneratedAt = DateTime.UtcNow.AddHours(7),
                TenantName = tenant?.BusinessName ?? tenant?.Name ?? "Restaurant",
                TenantAddress = tenant?.BusinessAddressLine1,
                TenantPhone = tenant?.BusinessPrimaryPhone,
                Summary = summary,
                RevenueTrend = revenueTrend,
                OrderTrend = orderTrend,
                TopDishes = topDishes,
                CustomerStats = customerStats,
                Promotions = promotions
            };
        }

        public Task<byte[]> GeneratePdfAsync(ReportData data)
        {
            var document = ReportDocumentFactory.Create(data);
            return Task.FromResult(document.GeneratePdf());
        }

        public async Task<byte[]> ExportAsync(ReportRequest request)
        {
            var data = await PrepareDataAsync(request);
            return await GeneratePdfAsync(data);
        }
        private DashboardRequest MapToDashboardRequest(ReportRequest request)
        {
            if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                return new DashboardRequest
                {
                    FilterType = MapFilterType(request.ReportType),
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                };
            }

            return new DashboardRequest { FilterType = MapFilterType(request.ReportType) };
        }

        private static string MapFilterType(string reportType) => reportType.ToLower() switch
        {
            "weekly" => "week",
            "monthly" => "month",
            "quarterly" => "quarter",
            "yearly" => "year",
            _ => "month"
        };

        private (DateTime from, DateTime to) ResolveDateRange(ReportRequest request)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;

            if (request.FromDate.HasValue && request.ToDate.HasValue)
                return (request.FromDate.Value.Date, request.ToDate.Value.Date.AddDays(1));

            return request.ReportType.ToLower() switch
            {
                "weekly" => (today.AddDays(-7), today.AddDays(1)),
                "monthly" => (today.AddDays(-30), today.AddDays(1)),
                "quarterly" => (today.AddMonths(-3), today.AddDays(1)),
                "yearly" => (today.AddDays(-365), today.AddDays(1)),
                _ => (today.AddDays(-30), today.AddDays(1))
            };
        }

        private static string BuildPeriodLabel(string reportType, DateTime from, DateTime to)
        {
            return reportType.ToLower() switch
            {
                "weekly" => $"Tuần: {from:dd/MM/yyyy} – {to.AddDays(-1):dd/MM/yyyy}",
                "monthly" => $"Tháng: {from:MM/yyyy} – {to.AddDays(-1):MM/yyyy}",
                "quarterly" => $"Quý: {from:MM/yyyy} – {to.AddDays(-1):MM/yyyy}",
                "yearly" => $"Năm: {from:yyyy} – {to.AddDays(-1):yyyy}",
                _ => $"{from:dd/MM/yyyy} – {to.AddDays(-1):dd/MM/yyyy}"
            };
        }

    }
}

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestX.BLL.DataTranferObjects.Reports;

namespace RestX.BLL.Services.Reports
{
    internal class WeeklyReportDocument : IDocument
    {
        private readonly ReportData _data;

        public WeeklyReportDocument(ReportData data) => _data = data;

        public DocumentMetadata GetMetadata() => new()
        {
            Title = $"Báo cáo tuần – {_data.TenantName}",
            Author = _data.TenantName,
            Subject = _data.PeriodLabel,
            CreationDate = _data.GeneratedAt
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextPrimary));

                page.Content().Column(col =>
                {
                    ReportComponents.RenderHeader(col, _data);
                    ReportComponents.SectionTitle(col, "Tổng quan tuần", _data.PeriodLabel);
                    ReportComponents.RenderKpiCards(col, _data.Summary);
                    ReportComponents.SectionTitle(col, "Chi tiết doanh thu & đơn hàng");
                    ReportComponents.RenderRevenueBreakdown(col, _data.Summary);
                    if (ReportComponents.HasRevenueTrendData(_data.RevenueTrend))
                    {
                        ReportComponents.SectionTitle(col, "Xu hướng doanh thu theo ngày",
                            $"Tổng: {ReportStyles.FormatCurrency(_data.RevenueTrend.TotalRevenue)}");
                        ReportComponents.RenderRevenueTrendTable(col, _data.RevenueTrend);
                    }
                    if (ReportComponents.HasOrderTrendData(_data.OrderTrend))
                    {
                        ReportComponents.SectionTitle(col, "Xu hướng đơn hàng theo ngày",
                            $"Tổng: {_data.OrderTrend.TotalOrders:N0} đơn");
                        ReportComponents.RenderOrderTrendTable(col, _data.OrderTrend);
                    }
                    if (ReportComponents.HasTopDishesData(_data.TopDishes))
                    {
                        ReportComponents.SectionTitle(col, "Top món bán chạy trong tuần");
                        ReportComponents.RenderTopDishes(col, _data.TopDishes);
                    }
                    if (ReportComponents.HasPromotionData(_data.Promotions))
                    {
                        ReportComponents.SectionTitle(col, "Khuyến mãi & giảm giá");
                        ReportComponents.RenderPromotionStats(col, _data.Promotions);
                    }
                    if (ReportComponents.HasCustomerData(_data.CustomerStats))
                    {
                        ReportComponents.SectionTitle(col, "Khách hàng");
                        ReportComponents.RenderCustomerStats(col, _data.CustomerStats);
                    }
                });

                ReportComponents.RenderFooter(page, _data);
            });
        }
    }
}

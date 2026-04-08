using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestX.BLL.DataTranferObjects.Reports;

namespace RestX.BLL.Services.Reports
{
    internal class MonthlyReportDocument : IDocument
    {
        private readonly ReportData _data;

        public MonthlyReportDocument(ReportData data) => _data = data;

        public DocumentMetadata GetMetadata() => new()
        {
            Title = $"Báo cáo tháng – {_data.TenantName}",
            Author = _data.TenantName,
            Subject = _data.PeriodLabel,
            CreationDate = _data.GeneratedAt
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            // Page 1 — Overview + Revenue
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextPrimary));

                page.Content().Column(col =>
                {
                    ReportComponents.RenderHeader(col, _data);

                    ReportComponents.SectionTitle(col, "Tổng quan tháng", _data.PeriodLabel);
                    ReportComponents.RenderKpiCards(col, _data.Summary);

                    ReportComponents.SectionTitle(col, "Chi tiết doanh thu & đơn hàng");
                    ReportComponents.RenderRevenueBreakdown(col, _data.Summary);

                    ReportComponents.SectionTitle(col, "Xu hướng doanh thu theo ngày",
                        $"Tổng: {ReportStyles.FormatCurrency(_data.RevenueTrend.TotalRevenue)}");
                    ReportComponents.RenderRevenueTrendTable(col, _data.RevenueTrend);
                });

                ReportComponents.RenderFooter(page, _data);
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextPrimary));

                page.Content().Column(col =>
                {
                    ReportComponents.SectionTitle(col, "Xu hướng đơn hàng theo ngày",
                        $"Tổng: {_data.OrderTrend.TotalOrders:N0} đơn");
                    ReportComponents.RenderOrderTrendTable(col, _data.OrderTrend);

                    ReportComponents.SectionTitle(col, "Top 10 món bán chạy trong tháng");
                    ReportComponents.RenderTopDishes(col, _data.TopDishes);

                    ReportComponents.SectionTitle(col, "Khách hàng tháng này");
                    ReportComponents.RenderCustomerStats(col, _data.CustomerStats);

                    if (_data.Promotions.TotalUsageCount > 0)
                    {
                        ReportComponents.SectionTitle(col, "Khuyến mãi & giảm giá");
                        ReportComponents.RenderPromotionStats(col, _data.Promotions);
                    }
                });

                ReportComponents.RenderFooter(page, _data);
            });
        }
    }
}

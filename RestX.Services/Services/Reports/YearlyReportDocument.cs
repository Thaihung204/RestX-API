using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestX.BLL.DataTranferObjects.Reports;

namespace RestX.BLL.Services.Reports
{
    internal class YearlyReportDocument : IDocument
    {
        private readonly ReportData _data;

        public YearlyReportDocument(ReportData data) => _data = data;

        public DocumentMetadata GetMetadata() => new()
        {
            Title = $"Báo cáo năm – {_data.TenantName}",
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

                    ReportComponents.SectionTitle(col, "Tổng quan năm", _data.PeriodLabel);
                    ReportComponents.RenderKpiCards(col, _data.Summary);

                    ReportComponents.SectionTitle(col, "Chi tiết doanh thu & đơn hàng");
                    ReportComponents.RenderRevenueBreakdown(col, _data.Summary);
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
                    ReportComponents.SectionTitle(col, "Doanh thu theo tháng",
                        $"Tổng: {ReportStyles.FormatCurrency(_data.RevenueTrend.TotalRevenue)}");
                    ReportComponents.RenderRevenueTrendTable(col, _data.RevenueTrend);

                    ReportComponents.SectionTitle(col, "Đơn hàng theo tháng",
                        $"Tổng: {_data.OrderTrend.TotalOrders:N0} đơn");
                    ReportComponents.RenderOrderTrendTable(col, _data.OrderTrend);
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
                    ReportComponents.SectionTitle(col, "Top 15 món bán chạy trong năm");
                    ReportComponents.RenderTopDishes(col, _data.TopDishes);

                    ReportComponents.SectionTitle(col, "Khách hàng năm nay");
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

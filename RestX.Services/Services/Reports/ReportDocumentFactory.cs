using QuestPDF.Infrastructure;
using RestX.BLL.DataTranferObjects.Reports;

namespace RestX.BLL.Services.Reports
{
    internal static class ReportDocumentFactory
    {
        static ReportDocumentFactory()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static IDocument Create(ReportData data) => data.ReportType.ToLower() switch
        {
            "weekly" => new WeeklyReportDocument(data),
            "monthly" => new MonthlyReportDocument(data),
            "quarterly" => new QuarterlyReportDocument(data),
            "yearly" => new YearlyReportDocument(data),
            _ => new MonthlyReportDocument(data)
        };
    }
}

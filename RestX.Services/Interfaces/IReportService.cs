using RestX.BLL.DataTranferObjects.Reports;

namespace RestX.BLL.Interfaces
{
    public interface IReportService
    {
        Task<ReportData> PrepareDataAsync(ReportRequest request);
        Task<byte[]> GeneratePdfAsync(ReportData data);
        Task<byte[]> ExportAsync(ReportRequest request);
    }
}

namespace RestX.BLL.DataTranferObjects.Reports
{
    public class ReportRequest
    {
        public string ReportType { get; set; } = "monthly";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TopDishesCount { get; set; } = 10;
    }
}

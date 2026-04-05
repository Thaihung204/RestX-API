namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class DashboardRequest
    {        public string FilterType { get; set; } = "week";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}

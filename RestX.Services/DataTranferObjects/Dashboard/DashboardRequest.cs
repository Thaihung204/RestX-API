namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class DashboardRequest
    {        public string FilterType { get; set; } = "today";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}

namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class OrderTrend
    {
        public string FilterType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalOrders { get; set; }
        public List<TrendPoint> OrderTrends { get; set; } = new();

        public class TrendPoint
        {
            public string Label { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public int Total { get; set; }
        }
    }
}

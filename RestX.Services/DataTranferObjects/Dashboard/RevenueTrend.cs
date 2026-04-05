namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class RevenueTrend
    {
        public string FilterType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<TrendPoint> RevenueTrends { get; set; } = new();

        public class TrendPoint
        {
            public string Label { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public decimal Value { get; set; }
        }
    }
}

namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class CancellationAnalysis
    {
        public int TotalCancelled { get; set; }
        public int TotalOrders { get; set; }
        public double CancelRate { get; set; }
        public List<CancelByHour> ByHour { get; set; } = new();
        public List<CancelByDow> ByDayOfWeek { get; set; } = new();
    }

    public class CancelByHour
    {
        public int Hour { get; set; }
        public int Count { get; set; }
    }

    public class CancelByDow
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

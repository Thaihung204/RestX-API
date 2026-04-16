namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class PeakHoursData
    {
        public List<PeakHourPoint> Points { get; set; } = new();
        public int PeakHour { get; set; }
        public string PeakDayOfWeek { get; set; } = string.Empty;
        public int OffPeakHour { get; set; }
    }

    public class PeakHourPoint
    {
        public int Hour { get; set; }
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
    }
}

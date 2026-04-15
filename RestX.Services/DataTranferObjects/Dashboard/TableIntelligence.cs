namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class TableIntelligence
    {
        public List<TablePerformance> Tables { get; set; } = new();
    }

    public class TablePerformance
    {
        public Guid TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public int SessionCount { get; set; }
        public double AvgSessionMinutes { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

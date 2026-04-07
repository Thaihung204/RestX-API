namespace RestX.BLL.DataTranferObjects.Dashboard
{
    // Generic DTOs cho SQL query results
    public class QueryResult
    {
        public class StatusCount
        {
            public string Status { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class OrderStatusCount
        {
            public int? Open { get; set; }
            public int? Completed { get; set; }
            public int? Cancelled { get; set; }
            public int? Total { get; set; }
        }

        public class TrendPoint
        {
            public DateTime Date { get; set; }
            public decimal? Value { get; set; }
            public int? Total { get; set; }
        }

        public class TableCount
        {
            public int Total { get; set; }
            public int Available { get; set; }
            public int Occupied { get; set; }
        }

        public class CustomerCount
        {
            public int Count { get; set; }
        }

        public class OrderRevenue
        {
            public int TotalOrders { get; set; }
            public decimal TotalRevenue { get; set; }
        }

    }
}

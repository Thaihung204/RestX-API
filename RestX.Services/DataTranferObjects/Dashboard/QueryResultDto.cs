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
            public int? Pending { get; set; }
            public int? Confirmed { get; set; }
            public int? Serving { get; set; }
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

        public class DishItem
        {
            public Guid DishId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Revenue { get; set; }
        }

        public class TableCount
        {
            public int Total { get; set; }
            public int Available { get; set; }
            public int Occupied { get; set; }
            public int Reserved { get; set; }
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

        public class TopCustomer
        {
            public Guid CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public int LoyaltyPoints { get; set; }
            public string MembershipLevel { get; set; } = string.Empty;
            public decimal TotalSpent { get; set; }
        }
    }
}

namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class CustomerStats
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageRevenuePerCustomer { get; set; }
        public double ChangePercent { get; set; }
        public bool IsTopCustomersFallback { get; set; }
        public List<TopCustomer> TopCustomers { get; set; } = new();

        public class TopCustomer
        {
            public int Rank { get; set; }
            public Guid CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public int LoyaltyPoints { get; set; }
            public string MembershipLevel { get; set; } = string.Empty;
            public decimal TotalSpent { get; set; }
        }
    }
}

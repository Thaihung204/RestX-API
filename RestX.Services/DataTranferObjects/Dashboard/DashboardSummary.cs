namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class DashboardSummary
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public RevenueSummary Revenue { get; set; } = new();
        public OrderSummary Orders { get; set; } = new();
        public ReservationSummary Reservations { get; set; } = new();
        public CustomerSummary NewCustomers { get; set; } = new();

        public class RevenueSummary
        {
            public decimal Total { get; set; }
            public double ChangePercent { get; set; }
        }

        public class OrderSummary
        {
            public int Total { get; set; }
            public int Open { get; set; }
            public int Completed { get; set; }
            public int Cancelled { get; set; }
            public int LiveProcessing { get; set; }
        }

        public class ReservationSummary
        {
            public int Total { get; set; }
            public int Pending { get; set; }
            public int Confirmed { get; set; }
            public int Cancelled { get; set; }
            public int LiveServing { get; set; }
        }

        public class CustomerSummary
        {
            public int Total { get; set; }
            public double ChangePercent { get; set; }
        }
    }
}

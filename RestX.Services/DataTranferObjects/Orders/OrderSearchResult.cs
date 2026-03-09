namespace RestX.BLL.DataTranferObjects.Orders
{
    public class OrderSearchResult
    {
        public List<Order> Orders { get; set; } = new();

        public int TotalCount { get; set; }

        public int Page { get; set; }
        public int ItemsPerPage { get; set; }

        public int TotalPages { get; set; }
    }
}
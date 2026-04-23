using RestX.Models.Enum;

namespace RestX.BLL.DataTranferObjects.Orders
{
    public class OrderSearch
    {
        // Paging
        public int Page { get; set; } = 1;
        public int ItemsPerPage { get; set; } = 20;

        // Date range
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // Filters by field name directly from query string
        public OrderStatus? Status { get; set; }
        public string? CustomerName { get; set; }
        public string? Reference { get; set; }
        public int? ItemCount { get; set; }
        public decimal? Total { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public DateTime? Time { get; set; }

        // Sort
        public string? SortBy { get; set; } = "created_desc";
    }
}
using RestX.Models.Enum;

namespace RestX.BLL.DataTranferObjects.Orders
{
    public class OrderSearch
    {
        // Paging
        public int Page { get; set; } = 1;
        public int ItemsPerPage { get; set; } = 20;

        // Filters
        public OrderStatus? Status { get; set; }

        /// <summary>
        /// Filter from date (inclusive). If only date is provided, treated as 00:00:00.
        /// </summary>
        public DateTime? From { get; set; }

        /// <summary>
        /// Filter to date (inclusive by day). Implemented as &lt; (To.Date + 1 day).
        /// </summary>
        public DateTime? To { get; set; }

        // Sort
        /// <summary>
        /// created_desc (default) | created_asc
        /// </summary>
        public string? SortBy { get; set; } = "created_desc";
    }
}
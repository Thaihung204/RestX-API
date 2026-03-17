namespace RestX.BLL.DataTranferObjects.Combo
{
    public class ComboSearch
    {
        public ComboSearch()
        {
            Page = 1;
            ItemsPerPage = 10;
        }

        // Paging
        public int Page { get; set; }
        public int ItemsPerPage { get; set; }

        // Search
        public string? SearchText { get; set; }

        // Filters
        public bool? IsActive { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }

        // Sort
        public string? SortBy { get; set; }
    }

    public class ComboSearchResult
    {
        public List<ComboSummary> Combos { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int Page { get; set; }
        public int ItemsPerPage { get; set; }
    }
}

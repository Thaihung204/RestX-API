public class DishSearch
{
    public DishSearch()
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
    public Guid? CategoryId { get; set; }
    public bool? IsVegetarian { get; set; }
    public bool? IsSpicy { get; set; }
    public bool? IsBestSeller { get; set; }
    public bool? IsActive { get; set; }

    public decimal? PriceFrom { get; set; }
    public decimal? PriceTo { get; set; }

    // Sort
    public string? SortBy { get; set; }
}

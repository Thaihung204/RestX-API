using RestX.BLL.DataTranferObjects.Dish;

public class DishItem
{
    // ===== Identity =====
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    // ===== Basic Info =====
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;

    // ===== Stock / Status =====
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
    public bool AutoDisableByStock { get; set; }

    // ===== Attributes =====
    public bool IsVegetarian { get; set; }
    public bool IsSpicy { get; set; }
    public bool IsBestSeller { get; set; }

    // ===== Images =====
    public string? MainImageUrl { get; set; }
    public List<DishImageItem> SubImages { get; set; } = new();

    // ===== Audit =====
    public DateTime CreatedDate { get; set; }
}

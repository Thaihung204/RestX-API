using Microsoft.AspNetCore.Http;

namespace RestX.BLL.DataTranferObjects.Combo
{
    public class ComboSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public IFormFile? File { get; set; }
        public decimal BaseCost { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public List<ComboDetailItem> Details { get; set; } = new();
    }
}
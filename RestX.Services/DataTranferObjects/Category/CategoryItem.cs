using Microsoft.AspNetCore.Http;

namespace RestX.BLL.DataTranferObjects.Category
{
    public class CategoryItem
    {
        public Guid? Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public IFormFile? File { get; set; }

        public string? ImageUrl { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }

        public List<CategoryItem>? CategoryChildrens { get; set; } = new();
    }
}
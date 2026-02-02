using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Category
{
    public class CategoryItem
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsActive { get; set; }

        public List<CategoryItem> CategoryChildrens { get; set; } = new();
    }
}

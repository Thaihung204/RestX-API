using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestX.Models.Inventory
{
    public partial class IngredientCategory : Entity<Guid>
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual ICollection<Ingredient> Ingredients { get; set; } = new HashSet<Ingredient>();
    }
}

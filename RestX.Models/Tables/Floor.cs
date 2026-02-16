using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestX.Models.Tables
{
    public partial class Floor : Entity<Guid>
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        [Column(TypeName = "decimal(8,2)")]
        [Range(0, 999999.99)]
        public decimal Width { get; set; } = 1400;
        [Column(TypeName = "decimal(8,2)")]
        [Range(0, 999999.99)]
        public decimal Height { get; set; } = 900;
        [MaxLength(500)]
        [Url]
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual ICollection<Table> Tables { get; set; } = new HashSet<Table>();
    }
}

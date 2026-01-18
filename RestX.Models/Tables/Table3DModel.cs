using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Tables
{
    public partial class Table3DModel : Entity<Guid>
    {
        public Guid TableId { get; set; }

        [Required]
        [MaxLength(500)]
        [Url]
        public string ModelUrl { get; set; } = string.Empty;

        [MaxLength(10)]
        public string ModelFormat { get; set; } = "GLB"; 

        [MaxLength(500)]
        [Url]
        public string? EnvironmentMapUrl { get; set; }

        [MaxLength(7)]
        public string BackgroundColor { get; set; } = "#FFFFFF";

        [Column(TypeName = "decimal(8,4)")]
        [Range(-999.9999, 999.9999)]
        public decimal CameraX { get; set; } = 0;

        [Column(TypeName = "decimal(8,4)")]
        [Range(-999.9999, 999.9999)]
        public decimal CameraY { get; set; } = 0;

        [Column(TypeName = "decimal(8,4)")]
        [Range(-999.9999, 999.9999)]
        public decimal CameraZ { get; set; } = 5;

        [Column(TypeName = "decimal(5,2)")]
        [Range(1, 120)]
        public decimal CameraFOV { get; set; } = 45;

        public bool AllowRotation { get; set; } = true;

        public bool AllowZoom { get; set; } = true;

        [Column(TypeName = "decimal(4,2)")]
        [Range(0.1, 10)]
        public decimal MinZoom { get; set; } = 0.5m;

        [Column(TypeName = "decimal(4,2)")]
        [Range(0.1, 10)]
        public decimal MaxZoom { get; set; } = 3;

       
        public virtual Table Table { get; set; } = null!;
    }
}

using RestX.Models.Attributes;
using RestX.Models.BaseModel;
using RestX.Models.Enum;
using RestX.Models.Orders;
using RestX.Models.Reservations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Tables
{
    public partial class Table : Entity<Guid>
    {
        public Guid FloorId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty;

        [Range(1, 50)]
        public int SeatingCapacity { get; set; } = 4;

        [Required]
        [MaxLength(20)]
        public string Shape { get; set; } = string.Empty;

        [Column(TypeName = "decimal(8,2)")]
        [Range(0, 999999.99)]
        public decimal PositionX { get; set; } = 0;

        [Column(TypeName = "decimal(8,2)")]
        [Range(0, 999999.99)]
        public decimal PositionY { get; set; } = 0;

        [Column(TypeName = "decimal(6,2)")]
        [Range(0, 9999.99)]
        public decimal Width { get; set; } = 100;

        [Column(TypeName = "decimal(6,2)")]
        [Range(0, 9999.99)]
        public decimal Height { get; set; } = 100;

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 360)]
        public decimal Rotation { get; set; } = 0;

        public bool Has3DView { get; set; } = false;

        [MaxLength(500)]
        public string? ViewDescription { get; set; }

        [MaxLength(500)]
        [Url]
        public string? DefaultViewUrl { get; set; }

        [MaxLength(500)]
        [Url]
        public string? CubeFrontImageUrl { get; set; }

        [MaxLength(500)]
        [Url]
        public string? CubeBackImageUrl { get; set; }

        [MaxLength(500)]
        [Url]
        public string? CubeLeftImageUrl { get; set; }

        [MaxLength(500)]
        [Url]
        public string? CubeRightImageUrl { get; set; }

        [MaxLength(500)]
        [Url]
        public string? CubeTopImageUrl { get; set; }

        [MaxLength(500)]
        [Url]
        public string? CubeBottomImageUrl { get; set; }

        [TriggerProperty(DisplayName = "Status")]
        public TableStatus TableStatusId { get; set; }

        public string? QRCodeUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public virtual Floor Floor { get; set; }
        public virtual Table3DModel? Table3DModel { get; set; }
        public virtual ICollection<TableSession> TableSessions { get; set; } = new HashSet<TableSession>();
        public virtual ICollection<ReservationTable> ReservationTables { get; set; } = new HashSet<ReservationTable>();
        public virtual ICollection<OrderTable> OrderTables { get; set; } = new HashSet<OrderTable>();
    }
}

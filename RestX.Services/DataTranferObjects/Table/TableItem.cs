using Microsoft.AspNetCore.Http;
using RestX.Models.Enum;
using System;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Table
{
    public class TableItem
    {
        public Guid Id { get; set; }

        public Guid FloorId { get; set; }
        public string FloorName { get; set; } = string.Empty;

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
        [Range(0, 999999.99)]
        public decimal PositionX { get; set; } = 0;
        [Range(0, 999999.99)]
        public decimal PositionY { get; set; } = 0;
        [Range(0, 9999.99)]
        public decimal Width { get; set; } = 100;
        [Range(0, 9999.99)]
        public decimal Height { get; set; } = 100;
        [Range(0, 360)]
        public decimal Rotation { get; set; } = 0;
        public bool Has3DView { get; set; } = false;
        [MaxLength(500)]
        public string? ViewDescription { get; set; }
        [MaxLength(500)]
        [Url]
        public string? DefaultViewUrl { get; set; }

        // --- 6 PROPERTIES CUBE URL ---
        public string? CubeFrontImageUrl { get; set; }
        public string? CubeBackImageUrl { get; set; }
        public string? CubeLeftImageUrl { get; set; }
        public string? CubeRightImageUrl { get; set; }
        public string? CubeTopImageUrl { get; set; }
        public string? CubeBottomImageUrl { get; set; }

        // --- 6 PROPERTIES CUBE FILE UPLOAD ---
        public IFormFile? CubeFrontImage { get; set; }
        public IFormFile? CubeBackImage { get; set; }
        public IFormFile? CubeLeftImage { get; set; }
        public IFormFile? CubeRightImage { get; set; }
        public IFormFile? CubeTopImage { get; set; }
        public IFormFile? CubeBottomImage { get; set; }
        public TableStatus TableStatusId { get; set; }
        public string? TableStatusName { get; set; }
        public string? QRCodeUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

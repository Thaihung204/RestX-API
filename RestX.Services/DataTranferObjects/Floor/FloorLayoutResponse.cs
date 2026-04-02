using System;
using System.Collections.Generic;

namespace RestX.BLL.DataTranferObjects.Floor
{
    public class FloorLayoutResponse
    {
        public FloorLayoutInfo Floor { get; set; } = null!;
        public List<TableLayoutItem> Tables { get; set; } = new();
    }

    public class FloorLayoutInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string? BackgroundImageUrl { get; set; }
    }

    public class TableLayoutItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int SeatingCapacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public TableLayoutPosition Layout { get; set; } = null!;
        public string? CubeFrontImageUrl { get; set; }
        public string? CubeBackImageUrl { get; set; }
        public string? CubeLeftImageUrl { get; set; }
        public string? CubeRightImageUrl { get; set; }
        public string? CubeTopImageUrl { get; set; }
        public string? CubeBottomImageUrl { get; set; }
    }

    public class TableLayoutPosition
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Rotation { get; set; }
        public string Shape { get; set; } = string.Empty;
    }
}

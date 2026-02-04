using System;

namespace RestX.BLL.DataTranferObjects.Table
{
    public class TableItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int SeatingCapacity { get; set; }
        public string Shape { get; set; } = string.Empty;
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Rotation { get; set; }
        public bool Has3DView { get; set; }
        public string? ViewDescription { get; set; }
        public string? DefaultViewUrl { get; set; }
        public Guid TableStatusId { get; set; }
        public string? TableStatusName { get; set; }
        public bool IsActive { get; set; }
    }
}

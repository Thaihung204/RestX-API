using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Floor
{
    public class SaveLayoutRequest
    {
        [Required]
        public List<SaveLayoutTableItem> Tables { get; set; } = new();
    }

    public class SaveLayoutTableItem
    {
        [Required]
        public Guid Id { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Rotation { get; set; }
    }
}

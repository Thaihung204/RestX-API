using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Status
{
    public class StatusValues
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(7)]
        public string ColorCode { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public int DisplayOrder { get; set; } = 1;
    }
}
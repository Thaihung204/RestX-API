using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RestX.BLL.DataTranferObjects.Floor
{
    public class Floor
    {
        [BindNever]
        public Guid? Id { get; set; }
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        [Range(0, 999999.99)]
        public decimal Width { get; set; } = 1400;
        [Range(0, 999999.99)]
        public decimal Height { get; set; } = 900;
        [BindNever]
        public string ImageUrl { get; set; } = string.Empty;
        [JsonIgnore]
        public IFormFile? Image { get; set; }
        public bool IsActive { get; set; } = true;
        [BindNever]
        public int TableCount { get; set; } = 0;
    }
}

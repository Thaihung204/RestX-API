using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Restaurant.Notifications
{
    public partial class Notification : Entity<Guid>
    {
        [MaxLength(450)]
        public string? RecipientId { get; set; }

        [MaxLength(20)]
        public string NotificationType { get; set; } = "INFO";

        public bool IsBroadcast { get; set; } = false;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(500)]
        [Url]
        public string? ImageUrl { get; set; }

        [MaxLength(10)]
        public string Priority { get; set; } = "NORMAL"; 

        public bool IsPublished { get; set; } = false;

        public DateTime? ExpiryDate { get; set; }
    }
}

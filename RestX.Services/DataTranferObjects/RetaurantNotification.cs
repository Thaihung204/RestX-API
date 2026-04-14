using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects
{
    public class RestaurantNotification
    {
        public Guid? Id { get; set; }

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

        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
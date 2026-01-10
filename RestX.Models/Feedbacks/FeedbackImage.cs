using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Feedbacks
{
    public partial class FeedbackImage : Entity<Guid>
    {
        public Guid FeedbackId { get; set; }

        [Required]
        [MaxLength(500)]
        [Url]
        public string ImageUrl { get; set; } = string.Empty;

        [Range(0, 999)]
        public int DisplayOrder { get; set; } = 0;

        public bool IsCover { get; set; } = false;

        public virtual Feedback Feedback { get; set; } = null!;
    }
}

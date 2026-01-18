using RestX.Models.BaseModel;
using RestX.Models.Customers;
using RestX.Models.Orders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.Feedbacks
{
    public partial class Feedback : Entity<Guid>
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public bool IsPublished { get; set; } = false;

        public bool IsAnonymous { get; set; } = false;

        public virtual Order Order { get; set; } = null!;
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<FeedbackImage> FeedbackImages { get; set; } = new HashSet<FeedbackImage>();
    }
}

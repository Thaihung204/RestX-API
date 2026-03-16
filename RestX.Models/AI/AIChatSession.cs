using RestX.Models.BaseModel;
using System.ComponentModel.DataAnnotations;

namespace RestX.Models.AI
{
    public class AIChatSession : Entity<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;
        public Guid? CustomerId { get; set; }
        public Guid? TableId { get; set; }

        public DateTime ExpiresAt { get; set; }
        public virtual ICollection<AIChatMessage> Messages { get; set; } = new HashSet<AIChatMessage>();
    }
}

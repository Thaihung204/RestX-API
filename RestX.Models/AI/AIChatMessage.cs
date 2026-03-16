using RestX.Models.BaseModel;
using System.ComponentModel.DataAnnotations;

namespace RestX.Models.AI
{
    public class AIChatMessage : Entity<Guid>
    {
        public Guid AIChatSessionId { get; set; }
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;
        [Required]
        public string Content { get; set; } = string.Empty;
        public virtual AIChatSession Session { get; set; } = null!;
    }
}

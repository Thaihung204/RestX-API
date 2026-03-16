using System.Text.Json.Serialization;

namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public Guid? TableId { get; set; }
        [JsonIgnore]
        public string? SessionId { get; set; }
        [JsonIgnore]
        public string? UserId { get; set; }
    }
}

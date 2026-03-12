namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public Guid? TableId { get; set; }
        public string? UserId { get; set; }
    }
}

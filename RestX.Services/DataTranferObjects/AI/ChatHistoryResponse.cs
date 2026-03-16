namespace RestX.BLL.DataTranferObjects.AI
{
    public class ChatHistoryResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public List<ChatHistoryItem> Messages { get; set; } = new();
    }

    public class ChatHistoryItem
    {
        public string Role { get; set; } = string.Empty; 
        public string Content { get; set; } = string.Empty;
        public AIChatResponse? Parsed { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIChatResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<AISuggestion> Suggestions { get; set; } = new();
        public List<AIComboSuggestion> Combos { get; set; } = new();
        public string UpsellHint { get; set; } = string.Empty;
        public List<string> QuickReplies { get; set; } = new();
        public AIOrderDraft? OrderDraft { get; set; }
        public Guid? CreatedOrderId { get; set; }
    }

    public class AIComboSuggestion
    {
        public Guid ComboId { get; set; }
        public string ComboName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<AISuggestion> Items { get; set; } = new();
        public decimal TotalPrice => Price > 0 ? Price : Items.Sum(i => i.TotalPrice);
    }

    public class AISuggestion
    {
        public Guid DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal TotalPrice => Price * Quantity;
        public string? ImageUrl { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<AIAction> Actions { get; set; } = new();
    }

    public class AIAction
    {
        public string Type { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}

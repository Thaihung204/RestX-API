namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIAnalyticsResponse
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow.AddHours(7);
        public string Summary { get; set; } = string.Empty;
        public List<AnalyticsInsight> Insights { get; set; } = new();
        public MenuAnalysis Menu { get; set; } = new();
        public CustomerAnalysis Customers { get; set; } = new();
        public List<ActionItem> ActionPlan { get; set; } = new();
    }

    // Hợp nhất: opportunity / risk / marketing — mọi insight đều có evidence (số liệu dẫn chứng)
    public class AnalyticsInsight
    {
        public string Category { get; set; } = string.Empty;  // opportunity | risk | marketing
        public string Title { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;  // số liệu cụ thể dẫn chứng
        public string Analysis { get; set; } = string.Empty;  // tại sao, ý nghĩa thực sự
        public string Action { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;    // high | medium
    }

    public class MenuAnalysis
    {
        public List<RankedDish> TopDishes { get; set; } = new();       // top đang bán tốt, có data
        public List<RankedDish> SuggestedDishes { get; set; } = new(); // đề xuất thêm mới (no1,no2,no3)
        public List<ComboSuggestion> CombosToCreate { get; set; } = new();
    }

    public class RankedDish
    {
        public int Rank { get; set; }
        public string DishName { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;  // "312 phần, +45% so kỳ trước, chiếm 28% DT"
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class ComboSuggestion
    {
        public int Rank { get; set; }
        public List<string> Dishes { get; set; } = new();
        public decimal? SuggestedPrice { get; set; }
        public string Evidence { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class CustomerAnalysis
    {
        public string Evidence { get; set; } = string.Empty;  // "45 khách mới (-8% so kỳ trước), 12 quay lại, TB 185k/khách"
        public string Insight { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class ActionItem
    {
        public int Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;    // high | medium
    }
}

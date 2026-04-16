namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIAnalyticsResponse
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow.AddHours(7);
        public string Summary { get; set; } = string.Empty;
        public int AlertCount { get; set; }
        public List<AIInsightItem> KeyInsights { get; set; } = new();
        public List<TopGrowthDriver> TopGrowthDrivers { get; set; } = new();
        public List<TopDeclineDriver> TopDeclineDrivers { get; set; } = new();
        public MenuDecisions MenuDecisions { get; set; } = new();
        public TopCustomerInsight? TopCustomer { get; set; }
        public PromotionInsight PromoInsight { get; set; } = new();
        public List<CustomerStrategyItem> CustomerStrategies { get; set; } = new();
        public List<ActionItem> Actions { get; set; } = new();
    }

    public class AIInsightItem
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    public class TopGrowthDriver
    {
        public string DishName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class TopDeclineDriver
    {
        public string DishName { get; set; } = string.Empty;
        public double ChangePercent { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class MenuDecisions
    {
        public KeepAndPushItem? KeepAndPush { get; set; }
        public List<ImproveOrRemoveItem> ImproveOrRemove { get; set; } = new();
        public List<SeasonalOpportunityItem> SeasonalOpportunities { get; set; } = new();
        public List<SuggestedAdditionItem> SuggestedAdditions { get; set; } = new();
        public List<ComboRecommendationItem> ComboRecommendations { get; set; } = new();
    }

    public class KeepAndPushItem
    {
        public string DishName { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class ImproveOrRemoveItem
    {
        public string DishName { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class SeasonalOpportunityItem
    {
        public string DishName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class SuggestedAdditionItem
    {
        public string DishName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class ComboRecommendationItem
    {
        public List<string> Dishes { get; set; } = new();
        public decimal? SuggestedPrice { get; set; }
        public decimal? AOVIncrease { get; set; }
    }
    public class TopCustomerInsight
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public string MembershipLevel { get; set; } = string.Empty;
        public string RevenueShare { get; set; } = string.Empty;
    }
    public class PromotionInsight
    {
        public decimal TotalCost { get; set; }
        public int TotalUsageCount { get; set; }
        public string Suggestion { get; set; } = string.Empty;
    }

    public class CustomerStrategyItem
    {
        public string Title { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string When { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class ActionItem
    {
        public string Title { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string When { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }
}
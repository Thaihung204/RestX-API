namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIAnalyticsResponse
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow.AddHours(7);
        public string Summary { get; set; } = string.Empty;
        public List<HiddenOpportunity> HiddenOpportunities { get; set; } = new();
        public List<HiddenRisk> HiddenRisks { get; set; } = new();
        public MenuStrategy MenuStrategy { get; set; } = new();
        public MarketingStrategy MarketingStrategy { get; set; } = new();
        public CustomerStrategy CustomerStrategy { get; set; } = new();
        public List<ActionItem> ActionPlan { get; set; } = new();
    }

    public class HiddenOpportunity
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Insight { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string When { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class HiddenRisk
    {
        public string Title { get; set; } = string.Empty;
        public string Insight { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string When { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }


    public class MenuStrategy
    {
        public TrendItem TrendingDish { get; set; } = new();

        public List<TimeBasedDish> TimeBasedDishes { get; set; } = new();

        public List<SuggestedDish> SuggestedAdditions { get; set; } = new();
        public List<ComboSuggestion> ComboSuggestions { get; set; } = new();
    }

    public class TrendItem
    {
        public string DishName { get; set; } = string.Empty;
        public string WhyTrending { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class TimeBasedDish
    {
        public string Context { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class SuggestedDish
    {
        public string DishName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class ComboSuggestion
    {
        public List<string> Dishes { get; set; } = new();
        public decimal? SuggestedPrice { get; set; }
        public decimal? AOVIncrease { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class MarketingStrategy
    {
        public string Trend { get; set; } = string.Empty;
        public string PromoStrategy { get; set; } = string.Empty;
        public List<MarketingAction> UpcomingActions { get; set; } = new();
    }

    public class MarketingAction
    {
        public string Title { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string When { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class CustomerStrategy
    {
        public List<CustomerAction> Actions { get; set; } = new();
    }

    public class CustomerAction
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
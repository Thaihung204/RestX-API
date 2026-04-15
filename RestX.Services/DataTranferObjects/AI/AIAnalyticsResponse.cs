namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIAnalyticsResponse
    {
        public string? FilterType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public OverviewSection Overview { get; set; } = new();
        public StrategySection Strategy { get; set; } = new();
        public RevenueSection Revenue { get; set; } = new();
        public MenuSection Menu { get; set; } = new();
        public CustomerSection Customers { get; set; } = new();
        public PromotionSection Promotions { get; set; } = new();
        public ActionSection Actions { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════════════
    // OVERVIEW
    // ══════════════════════════════════════════════════════════════════════
    public class OverviewSection
    {
        /// <summary>Tóm tắt 2-3 câu: tình hình chung + điểm nổi bật + việc khẩn nhất</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Tổng số cảnh báo cần chú ý (tự tính)</summary>
        public int AlertCount { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════
    // STRATEGY — Chiến lược kinh doanh
    // ══════════════════════════════════════════════════════════════════════
    public class StrategySection
    {
        /// <summary>Chiến lược ngắn hạn 1-2 tuần</summary>
        public List<StrategyItem> ShortTerm { get; set; } = new();

        /// <summary>Định hướng trung hạn 1-3 tháng</summary>
        public List<StrategyItem> MediumTerm { get; set; } = new();

        /// <summary>Đòn bẩy tăng trưởng — cơ hội lớn nếu khai thác đúng</summary>
        public List<GrowthLever> GrowthLevers { get; set; } = new();

        /// <summary>Rủi ro cần lưu ý</summary>
        public List<RiskItem> Risks { get; set; } = new();
    }

    public class StrategyItem
    {
        public string Title { get; set; } = string.Empty;
        /// <summary>Giải thích dựa trên data: tại sao, làm gì, ai làm</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>Kết quả kỳ vọng có số liệu</summary>
        public string ExpectedOutcome { get; set; } = string.Empty;
        /// <summary>high | medium | low</summary>
        public string Priority { get; set; } = string.Empty;
    }

    public class GrowthLever
    {
        public string Title { get; set; } = string.Empty;
        /// <summary>Cơ hội đến từ data nào, tại sao là đòn bẩy mạnh</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>+X% doanh thu / +Y đơn nếu khai thác đúng</summary>
        public string PotentialImpact { get; set; } = string.Empty;
        /// <summary>Bước đầu tiên làm được trong tuần này</summary>
        public string FirstStep { get; set; } = string.Empty;
    }

    public class RiskItem
    {
        /// <summary>Rủi ro cụ thể từ data hiện tại</summary>
        public string Risk { get; set; } = string.Empty;
        /// <summary>high | medium | low</summary>
        public string Severity { get; set; } = string.Empty;
        /// <summary>Cách giảm thiểu: ai làm, làm gì, trong bao lâu</summary>
        public string Mitigation { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════
    // REVENUE — Tại sao doanh thu thay đổi?
    // ══════════════════════════════════════════════════════════════════════
    public class RevenueSection
    {
        /// <summary>Yếu tố tăng trưởng</summary>
        public List<DriverItem> GrowthFactors { get; set; } = new();

        /// <summary>Yếu tố suy giảm — cần hành động</summary>
        public List<DriverItem> DeclineFactors { get; set; } = new();

        /// <summary>Ảnh hưởng bên ngoài: thời tiết, ngày lễ, mùa vụ</summary>
        public List<string> ExternalFactors { get; set; } = new();
    }

    public class DriverItem
    {
        /// <summary>Mô tả + giả thuyết tại sao + đã diễn ra bao lâu</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>% thay đổi so kỳ trước (dương = tăng, âm = giảm)</summary>
        public double ChangePercent { get; set; }
        /// <summary>normal | warning | critical</summary>
        public string Severity { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════
    // MENU
    // ══════════════════════════════════════════════════════════════════════
    public class MenuSection
    {
        public List<MenuDecisionItem> KeepAndPush { get; set; } = new();
        public List<MenuDecisionItem> ImproveOrRemove { get; set; } = new();
        public List<MenuDecisionItem> SeasonalOpportunities { get; set; } = new();
        public List<MenuDecisionItem> SuggestedAdditions { get; set; } = new();
        public List<ComboRecommendation> ComboRecommendations { get; set; } = new();
    }

    public class MenuDecisionItem
    {
        public string DishName { get; set; } = string.Empty;
        /// <summary>growing | stable | declining | new</summary>
        public string Trend { get; set; } = string.Empty;
        public double ChangePercent { get; set; }
        public decimal Revenue { get; set; }
        public int Quantity { get; set; }
        /// <summary>high | medium | low</summary>
        public string Priority { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public class ComboRecommendation
    {
        public List<string> Dishes { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
        public decimal? SuggestedPrice { get; set; }
        public decimal? AOVIncrease { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUSTOMERS
    // ══════════════════════════════════════════════════════════════════════
    public class CustomerSection
    {
        /// <summary>Tóm tắt: khách mới tăng/giảm %, VIP chi tiêu thế nào, rủi ro churn</summary>
        public string Summary { get; set; } = string.Empty;

        public List<TopCustomerInsight> TopCustomers { get; set; } = new();
        public List<string> RetentionSuggestions { get; set; } = new();
        public List<string> ChurnWarnings { get; set; } = new();
    }

    public class TopCustomerInsight
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        /// <summary>Bronze | Silver | Gold | Platinum</summary>
        public string MembershipLevel { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════
    // PROMOTIONS
    // ══════════════════════════════════════════════════════════════════════
    public class PromotionSection
    {
        /// <summary>Nhận xét tổng + kết luận: hiệu quả/bình thường/lãng phí/chưa có dữ liệu</summary>
        public string Summary { get; set; } = string.Empty;

        public decimal TotalCost { get; set; }
        public int TotalUsageCount { get; set; }

        public List<PromoROIItem> Promos { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    public class PromoROIItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public int UsageCount { get; set; }
        public decimal RevenueFromPromo { get; set; }
        public decimal CostPerOrder { get; set; }
        public string Reason { get; set; } = string.Empty;
        /// <summary>tiếp tục | chỉnh sửa | dừng</summary>
        public string RecommendedAction { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════
    // ACTIONS
    // ══════════════════════════════════════════════════════════════════════
    public class ActionSection
    {
        /// <summary>Làm NGAY — ảnh hưởng doanh thu trực tiếp</summary>
        public List<ActionItem> Urgent { get; set; } = new();

        /// <summary>Làm trong tuần này</summary>
        public List<ActionItem> ThisWeek { get; set; } = new();

        /// <summary>Cơ hội tăng trưởng tiềm năng</summary>
        public List<ActionItem> Opportunities { get; set; } = new();
    }

    public class ActionItem
    {
        public string Title { get; set; } = string.Empty;
        /// <summary>Tại sao — liên kết con số cụ thể từ data</summary>
        public string Reason { get; set; } = string.Empty;
        /// <summary>Làm gì cụ thể: ai, làm gì, deadline</summary>
        public string Action { get; set; } = string.Empty;
        /// <summary>Ngay | Tuần này | Tháng này</summary>
        public string When { get; set; } = string.Empty;
        /// <summary>high | medium | low</summary>
        public string Impact { get; set; } = string.Empty;
    }
}

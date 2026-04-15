namespace RestX.BLL.DataTranferObjects.AI
{
    /// <summary>
    /// Streamlined AI dashboard analysis response.
    /// Fields kept only if FE actually displays them.
    /// Remove: FilterType (FE already has), FromDate/ToDate (FE already sent),
    /// strategy (redundant with actions), promos[] (only need total cost),
    /// topCustomers[] (only need #1), growthLevers, risks.
    /// </summary>
    public class AIAnalyticsResponse
    {
        /// <summary>Thời điểm AI tạo xong phân tích</summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        // ══════════════════════════════════════════════════════════════════════
        // FE DISPLAY SECTION — Những gì dashboard thực sự hiển thị
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Tóm tắt 2-3 câu: doanh thu tăng/giảm bao nhiêu%, điểm nổi bật, việc khẩn nhất.
        /// FE hiển thị card riêng — nhưng summary giúp chủ nhà hàng đọc lướt nhanh.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Số cảnh báo cần chú ý (tự tính: urgent actions + churn + critical declines)</summary>
        public int AlertCount { get; set; }

        // ── keyInsights ─────────────────────────────────────────────────────
        // FE KHÔNG hiển thị trực tiếp — nhưng là thông tin giá trị AI phân tích ra.
        // Tối đa 3 items: warning / opportunity / info. Mỗi item ngắn gọn.
        public List<AIInsightItem> KeyInsights { get; set; } = new();

        // ── topGrowthDrivers ────────────────────────────────────────────────
        // FE có chart + menu table rồi — chỉ cần AI chỉ rõ ĐỘNG LỰC tăng trưởng chính.
        public List<TopGrowthDriver> TopGrowthDrivers { get; set; } = new();

        // ── topDeclineDrivers ───────────────────────────────────────────────
        // FE có chart rồi — chỉ cần AI cảnh báo rõ món nào đang giảm và mức độ.
        public List<TopDeclineDriver> TopDeclineDrivers { get; set; } = new();

        // ── menuDecisions ──────────────────────────────────────────────────
        // keepAndPush → topDish (FE: menu table đã hiển thị, chỉ cần AI gợi action)
        // improveOrRemove → list (FE: chart đã hiển thị, chỉ cần AI quyết định cụ thể)
        // seasonalOpportunities → list (FE: KHÔNG hiển thị, AI gợi ý món mùa)
        // suggestedAdditions → list (FE: KHÔNG hiển thị, AI gợi món phổ biến chưa có)
        // comboRecommendations → list (FE: KHÔNG hiển thị, AI gợi combo upsell)
        public MenuDecisions MenuDecisions { get; set; } = new();

        // ── topCustomer ────────────────────────────────────────────────────
        // FE KHÔNG hiển thị table VIP — chỉ cần top 1 customer + membership level.
        public TopCustomerInsight? TopCustomer { get; set; }

        // ── promotionInsight ───────────────────────────────────────────────
        // FE KHÔNG hiển thị table promo — chỉ cần tổng chi phí + gợi ý cải thiện.
        public PromotionInsight PromoInsight { get; set; } = new();

        // ── actions ────────────────────────────────────────────────────────
        // FE hiển thị dashboard rồi — actions là những việc cần làm sau khi đọc xong.
        // BỎ: shortTerm/mediumTerm trùng với thisWeek/opportunities
        // GIỮ: urgent + thisWeek + opportunities (gộp ngắn hạn + trung hạn)
        public ActionSection Actions { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════════════
    // SUB CLASSES
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>3 loại: warning / opportunity / info. Mỗi item ngắn, có title + detail.</summary>
    public class AIInsightItem
    {
        /// <summary>warning | opportunity | info</summary>
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    /// <summary>Động lực tăng trưởng — chỉ cần món chính + con số</summary>
    public class TopGrowthDriver
    {
        public string DishName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Quantity { get; set; }
        /// <summary>1 câu giải thích tại sao tăng</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>Yếu tố suy giảm — chỉ cần món đang giảm + mức độ</summary>
    public class TopDeclineDriver
    {
        public string DishName { get; set; } = string.Empty;
        public double ChangePercent { get; set; }
        /// <summary>normal | warning | critical</summary>
        public string Severity { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>Menu decisions: keep/bỏ/thêm/seasonal/combo — chỉ fields FE cần, bỏ priority/reason đầy đủ</summary>
    public class MenuDecisions
    {
        /// <summary>Món growing/stable — chỉ cần top 1 + action gợi ý</summary>
        public KeepAndPushItem? KeepAndPush { get; set; }

        /// <summary>Món declining — chỉ cần list tên + action</summary>
        public List<ImproveOrRemoveItem> ImproveOrRemove { get; set; } = new();

        /// <summary>Cơ hội theo mùa — FE KHÔNG hiển thị, AI gợi món phù hợp tháng hiện tại</summary>
        public List<SeasonalOpportunityItem> SeasonalOpportunities { get; set; } = new();

        /// <summary>Món phổ biến F&B nhà hàng CHƯA CÓ — FE KHÔNG hiển thị, AI gợi</summary>
        public List<SuggestedAdditionItem> SuggestedAdditions { get; set; } = new();

        /// <summary>Combo gợi ý — FE KHÔNG hiển thị, AI gợi upsell</summary>
        public List<ComboRecommendationItem> ComboRecommendations { get; set; } = new();
    }

    public class KeepAndPushItem
    {
        public string DishName { get; set; } = string.Empty;
        /// <summary>growing | stable</summary>
        public string Trend { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        /// <summary>1 câu: tại sao nên giữ</summary>
        public string Reason { get; set; } = string.Empty;
        /// <summary>1 câu: làm gì cụ thể (vd: tăng quảng cáo)</summary>
        public string Action { get; set; } = string.Empty;
    }

    public class ImproveOrRemoveItem
    {
        public string DishName { get; set; } = string.Empty;
        /// <summary>declining | zero</summary>
        public string Trend { get; set; } = string.Empty;
        /// <summary>1 câu: tại sao nên cải thiện / bỏ</summary>
        public string Reason { get; set; } = string.Empty;
        /// <summary>1 câu: làm gì cụ thể (vd: họp bếp, thử thay đổi công thức, loại khỏi menu)</summary>
        public string Action { get; set; } = string.Empty;
    }

    public class SeasonalOpportunityItem
    {
        public string DishName { get; set; } = string.Empty;
        /// <summary>1 câu: tại sao phù hợp mùa này</summary>
        public string Reason { get; set; } = string.Empty;
        /// <summary>1 câu: thử nghiệm như thế nào</summary>
        public string Action { get; set; } = string.Empty;
    }

    public class SuggestedAdditionItem
    {
        public string DishName { get; set; } = string.Empty;
        /// <summary>1 câu: tại sao nên thêm</summary>
        public string Reason { get; set; } = string.Empty;
    }

    public class ComboRecommendationItem
    {
        /// <summary>2 món trở lên</summary>
        public List<string> Dishes { get; set; } = new();
        public decimal? SuggestedPrice { get; set; }
        /// <summary>Ước tính tăng bao nhiêu/đơn</summary>
        public decimal? AOVIncrease { get; set; }
    }

    /// <summary>Top customer — FE KHÔNG hiển thị VIP table, chỉ cần top 1</summary>
    public class TopCustomerInsight
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        /// <summary>Bronze | Silver | Gold | Platinum</summary>
        public string MembershipLevel { get; set; } = string.Empty;
        /// <summary>Chiếm bao nhiêu % doanh thu</summary>
        public string RevenueShare { get; set; } = string.Empty;
    }

    /// <summary>Promotion insight — FE KHÔNG hiển thị promo table, chỉ cần tổng + gợi ý</summary>
    public class PromotionInsight
    {
        public decimal TotalCost { get; set; }
        public int TotalUsageCount { get; set; }
        /// <summary>1-2 câu: promo hiệu quả/chưa/kém</summary>
        public string Suggestion { get; set; } = string.Empty;
    }

    /// <summary>Actions to take — gộp shortTerm + mediumTerm vào thisWeek + opportunities</summary>
    public class ActionSection
    {
        /// <summary>Làm NGAY — ảnh hưởng doanh thu trực tiếp</summary>
        public List<ActionItem> Urgent { get; set; } = new();

        /// <summary>Làm trong tuần này — shortTerm + mediumTerm gộp lại</summary>
        public List<ActionItem> ThisWeek { get; set; } = new();

        /// <summary>Cơ hội tăng trưởng dài hạn hơn</summary>
        public List<ActionItem> Opportunities { get; set; } = new();
    }

    public class ActionItem
    {
        /// <summary>≤ 10 từ</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Con số cụ thể + tại sao khẩn</summary>
        public string Reason { get; set; } = string.Empty;
        /// <summary>AI làm gì + deadline</summary>
        public string Action { get; set; } = string.Empty;
        /// <summary>Ngay | Tuần này | Tháng này</summary>
        public string When { get; set; } = string.Empty;
        /// <summary>high | medium | low</summary>
        public string Impact { get; set; } = string.Empty;
    }
}
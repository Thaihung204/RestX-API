namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class PromotionStats
    {
        public decimal TotalDiscountAmount { get; set; }
        public int TotalUsageCount { get; set; }
        public List<PromotionUsageItem> TopPromotions { get; set; } = new();
    }

    public class PromotionUsageItem
    {
        public string PromotionCode { get; set; } = string.Empty;
        public string PromotionName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalDiscount { get; set; }
    }
}

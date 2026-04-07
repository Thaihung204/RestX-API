namespace RestX.BLL.DataTranferObjects.Orders
{
    public class ApplyDiscountResponse
    {
        public Guid OrderId { get; set; }
        public decimal SubTotal { get; set; }
        public DiscountBreakdown Breakdown { get; set; } = new();
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal TotalAmount { get; set; }

        public AppliedPromotionInfo? AppliedPromotion { get; set; }
        public string? PromotionError { get; set; }

        public AppliedMembershipInfo? AppliedMembership { get; set; }
    }

    public class DiscountBreakdown
    {
        public decimal PromotionDiscount { get; set; }
        public decimal MembershipDiscount { get; set; }
    }

    public class AppliedPromotionInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal MaxDiscountAmount { get; set; }
    }

    public class AppliedMembershipInfo
    {
        public string Level { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
    }
}

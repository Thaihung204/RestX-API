namespace RestX.BLL.DataTranferObjects.Orders
{
    public class ApplyDiscountRequest
    {
        public string? PromotionCode { get; set; }
        public bool ApplyMembership { get; set; } = true;
    }
}

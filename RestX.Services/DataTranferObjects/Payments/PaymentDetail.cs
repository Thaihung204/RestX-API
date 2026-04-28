using RestX.Models.Enum;

namespace RestX.BLL.DataTranferObjects.Payments
{
    public class PaymentDetail
    {
        public Guid Id { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? ReservationId { get; set; }
        public string PaymentMethodId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long? PayOSOrderCode { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? TransactionId { get; set; }
        public decimal CashReceive { get; set; }
        public decimal Cashback { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public PaymentPurpose Purpose { get; set; }
        public string PurposeName => Purpose.ToString();

        // GetAll
        public string? CustomerName { get; set; }

        // GetById only
        public PaymentCustomerInfo? Customer { get; set; }
        public PaymentOrderInfo? Order { get; set; }
        public PaymentReservationInfo? Reservation { get; set; }
        public decimal? DepositPaid { get; set; }
    }

    public class PaymentCustomerInfo
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string MembershipLevel { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
    }

    public class PaymentOrderInfo
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PaymentOrderItem> Items { get; set; } = new();
        public List<PaymentPromotionInfo> Promotions { get; set; } = new();
    }

    public class PaymentOrderItem
    {
        public Guid Id { get; set; }
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
        public string? ItemStatus { get; set; }
    }

    public class PaymentPromotionInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
    }

    public class PaymentReservationInfo
    {
        public Guid Id { get; set; }
        public string? ConfirmationCode { get; set; }
        public int NumberOfGuests { get; set; }
        public DateTime Time { get; set; }
        public string? SpecialRequests { get; set; }
        public decimal DepositAmount { get; set; }
    }
}

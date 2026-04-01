using CsvHelper.Configuration.Attributes;

namespace RestX.BLL.DataTranferObjects.Export
{
    public class OrderExportRow
    {
        [Name("Reference")]
        public string Reference { get; set; } = "";

        [Name("Order Status")]
        public string OrderStatus { get; set; } = "";

        [Name("Sub Total")]
        public decimal SubTotal { get; set; }

        [Name("Discount")]
        public decimal DiscountAmount { get; set; }

        [Name("Tax")]
        public decimal TaxAmount { get; set; }

        [Name("Service Charge")]
        public decimal ServiceCharge { get; set; }

        [Name("Total Amount")]
        public decimal TotalAmount { get; set; }

        [Name("Payment Status")]
        public string PaymentStatus { get; set; } = "";

        [Name("Item Count")]
        public int ItemCount { get; set; }

        [Name("Created Date")]
        public string CreatedDate { get; set; } = "";

        [Name("Completed At")]
        public string CompletedAt { get; set; } = "";

        [Name("Cancelled At")]
        public string CancelledAt { get; set; } = "";
    }
}

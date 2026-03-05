namespace RestX.BLL.Helpers
{
    public static class PaymentConstants
    {
        public static class StatusType
        {
            public const string Payment = "PAYMENT";
        }

        public static class StatusCode
        {
            public const string Paid = "PAID";
            public const string Unpaid = "UNPAID";
            public const string Cancelled = "CANCELLED";
        }

        public static class Method
        {
            public const string Cash = "CASH";
            public const string Bank = "BANK";
        }

        public static class SettingKey
        {
            public const string Payment = "payment";
        }
    }
}

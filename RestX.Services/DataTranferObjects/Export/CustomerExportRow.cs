using CsvHelper.Configuration.Attributes;

namespace RestX.BLL.DataTranferObjects.Export
{
    public class CustomerExportRow
    {
        [Name("Full Name")]
        public string FullName { get; set; } = "";

        [Name("Email")]
        public string Email { get; set; } = "";

        [Name("Phone Number")]
        public string PhoneNumber { get; set; } = "";

        [Name("Membership Level")]
        public string MembershipLevel { get; set; } = "";

        [Name("Loyalty Points")]
        public int LoyaltyPoints { get; set; }

        [Name("Total Orders")]
        public int TotalOrders { get; set; }

        [Name("Total Spent")]
        public decimal TotalSpent { get; set; }

        [Name("Last Visit")]
        public string LastVisit { get; set; } = "";

        [Name("Registered Date")]
        public string RegisteredDate { get; set; } = "";

        [Name("Status")]
        public string Status { get; set; } = "";
    }
}

using CsvHelper.Configuration.Attributes;

namespace RestX.BLL.DataTranferObjects.Export
{
    public class ReservationExportRow
    {
        [Name("Confirmation Code")]
        public string ConfirmationCode { get; set; } = "";

        [Name("Contact Name")]
        public string ContactName { get; set; } = "";

        [Name("Contact Phone")]
        public string ContactPhone { get; set; } = "";

        [Name("Reservation Date & Time")]
        public string ReservationDateTime { get; set; } = "";

        [Name("Number of Guests")]
        public int NumberOfGuests { get; set; }

        [Name("Tables")]
        public string Tables { get; set; } = "";

        [Name("Status")]
        public string Status { get; set; } = "";

        [Name("Deposit Amount")]
        public decimal DepositAmount { get; set; }

        [Name("Deposit Paid")]
        public string DepositPaid { get; set; } = "";

        [Name("Created At")]
        public string CreatedAt { get; set; } = "";
    }
}

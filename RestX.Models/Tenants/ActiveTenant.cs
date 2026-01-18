namespace RestX.Models.Tenants
{
    public class ActiveTenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ConnectionString { get; set; }
        public string Hostname { get; set; }
        public bool IsActive { get; set; }
        public string Prefix { get; set; }
        public string BaseColor { get; set; }
        public string PrimaryColor { get; set; }
        public string PrimaryDocsColor { get; set; }
        public string SecondaryColor { get; set; }
        public string HeaderColor { get; set; }
        public string FooterColor { get; set; }
        public string FaviconUrl { get; set; }
        public string LogoUrl { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Business Details
        public string BusinessName { get; set; }
        public string BusinessAddressLine1 { get; set; }
        public string BusinessAddressLine2 { get; set; }
        public string BusinessAddressLine3 { get; set; }
        public string BusinessAddressLine4 { get; set; }
        public string BusinessCounty { get; set; }
        public string BusinessPostCode { get; set; }
        public string BusinessCountry { get; set; }
        public string BusinessPrimaryPhone { get; set; }
        public string BusinessSecondaryPhone { get; set; }
        public string BusinessEmailAddress { get; set; }
        public string BusinessCompanyNumber { get; set; }
        public string BusinessOpeningHours { get; set; }
    }
}

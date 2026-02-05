using RestX.Models.Tenants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Tenants
{
    public class TenantOverview
    {
        public string Prefix { get; set; }

        public string Name { get; set; }

        public string LogoUrl { get; set; }

        public string FaviconUrl { get; set; }

        public string BackgroundUrl { get; set; }

        public string BaseColor { get; set; }

        public string PrimaryColor { get; set; }

        public string SecondaryColor { get; set; }
        public string HeaderColor { get; set; }
        public string FooterColor { get; set; }

        public string NetworkIp { get; set; }

        public string ConnectionString { get; set; }

        public bool Status { get; set; }

        public string Hostname { get; set; }

        public DateTime ExpiredAt { get; set; }
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

        public string AboutUs { get; set; }
        public virtual ICollection<TenantSetting> TenantSettings { get; set; } = new List<TenantSetting>();
    }
}

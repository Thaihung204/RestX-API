using RestX.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Tenants
{
    public class TenantRequest
    {
        public Guid? Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Hostname { get; set; }

        public string? BusinessName { get; set; }
        public string? BusinessPrimaryPhone { get; set; }

        [EmailAddress]
        public string? BusinessEmailAddress { get; set; }

        public string? BusinessAddressLine1 { get; set; }
        public string? BusinessAddressLine2 { get; set; }
        public string? BusinessAddressLine3 { get; set; }
        public string? BusinessAddressLine4 { get; set; }
        public string? BusinessCountry { get; set; }

        public TenantRequestStatus? tenantRequestStatus { get; set; }
    }
}
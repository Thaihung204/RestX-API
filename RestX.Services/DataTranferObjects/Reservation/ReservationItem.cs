using RestX.BLL.DataTranferObjects.Common;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Reservation
{
    public class CreateReservationRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one table must be selected")]
        public List<Guid> TableIds { get; set; } = new();
        [Required]
        public DateTime ReservationDateTime { get; set; }
        [Required]
        [Range(1, 100, ErrorMessage = "Number of guests must be between 1 and 100")]
        public int NumberOfGuests { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(15)]
        public string Phone { get; set; } = string.Empty;
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? SpecialRequests { get; set; }
    }

    public class UpdateReservationStatusRequest
    {
        [Required]
        public int StatusId { get; set; }
    }

    public class UpdateReservationRequest
    {
        public List<Guid>? TableIds { get; set; }
        public DateTime? ReservationDateTime { get; set; }
        [Range(1, 100, ErrorMessage = "Number of guests must be between 1 and 100")]
        public int? NumberOfGuests { get; set; }
        [MaxLength(1000)]
        public string? SpecialRequests { get; set; }
    }

    public class ReservationFilterParams : BaseFilterParams
    {
        public int? StatusId { get; set; }
        public DateTime? Date { get; set; }
        public Guid? TableId { get; set; }
    }

    public class CheckAvailabilityParams
    {
        [Required]
        public List<Guid> TableIds { get; set; } = new();
        [Required]
        public DateTime ReservationDateTime { get; set; }
        public int BufferMinutes { get; set; } = 120;
    }
    public class ReservationTableInfo
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string FloorName { get; set; } = string.Empty;
    }

    public class ReservationContactInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsGuest { get; set; }
        public Guid? CustomerId { get; set; }
        public string? MembershipLevel { get; set; }
        public int? LoyaltyPoints { get; set; }
    }

    public class ReservationStatusInfo
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColorCode { get; set; } = string.Empty;
    }

    public class ReservationListItem
    {
        public Guid Id { get; set; }
        public string ConfirmationCode { get; set; } = string.Empty;
        public List<ReservationTableInfo> Tables { get; set; } = new();
        public DateTime ReservationDateTime { get; set; }
        public int NumberOfGuests { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public bool IsGuest { get; set; }
        public ReservationStatusInfo Status { get; set; } = new();
        public decimal DepositAmount { get; set; }
        public bool DepositPaid { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReservationDetail
    {
        public Guid Id { get; set; }
        public string ConfirmationCode { get; set; } = string.Empty;
        public List<ReservationTableInfo> Tables { get; set; } = new();
        public DateTime ReservationDateTime { get; set; }
        public int NumberOfGuests { get; set; }
        public ReservationContactInfo Contact { get; set; } = new();
        public string? SpecialRequests { get; set; }
        public ReservationStatusInfo Status { get; set; } = new();
        public decimal DepositAmount { get; set; }
        public bool DepositPaid { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CheckAvailabilityResponse
    {
        public bool Available { get; set; }
        public List<ConflictingSlot> ConflictingSlots { get; set; } = new();
    }

    public class ConflictingSlot
    {
        public Guid TableId { get; set; }
        public string TableCode { get; set; } = string.Empty;
        public DateTime ConflictTime { get; set; }
        public string ConflictStatus { get; set; } = string.Empty;
    }
}

using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Reservation;

namespace RestX.BLL.Interfaces.Reservations
{
    public interface IReservationService
    {
        Task<ReservationDetail> CreateReservation(CreateReservationRequest request, Guid? applicationUserId);
        Task<PaginatedResult<ReservationListItem>> GetReservations(ReservationFilterParams filter);
        Task<PaginatedResult<ReservationListItem>> GetMyReservations(Guid applicationUserId, PaginationParams pagination);
        Task<ReservationDetail?> GetReservationById(Guid id);
        Task<ReservationDetail?> LookupReservation(string confirmationCode, string phone);
        Task<ReservationDetail> UpdateReservation(Guid id, UpdateReservationRequest request);
        Task UpdateReservationStatus(Guid id, int statusId);
        Task CancelReservation(Guid id);
        Task<CheckAvailabilityResponse> CheckAvailabilityReservation(CheckAvailabilityParams request);
    }
}

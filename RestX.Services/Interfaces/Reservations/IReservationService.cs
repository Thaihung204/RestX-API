using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Reservation;

namespace RestX.BLL.Interfaces.Reservations
{
    public interface IReservationService
    {
        Task<ReservationDetail> CreateReservation(CreateReservationRequest request);
        Task<PaginatedResult<ReservationListItem>> GetReservations(ReservationFilterParams filter);
        Task<PaginatedResult<ReservationListItem>> GetMyReservations(Guid applicationUserId, PaginationParams pagination);
        Task<ReservationDetail?> GetReservationById(Guid id);
        Task<ReservationDetail?> GetReservationByCode(string confirmationCode);
        Task<ReservationDetail> UpdateReservation(Guid id, UpdateReservationRequest request);
        Task CheckIn(Guid id);
        Task CancelReservation(Guid id);
        Task<CheckAvailabilityResponse> CheckAvailabilityReservation(CheckAvailabilityParams request);
    }
}

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
        Task ChangeStatus(Guid id, int statusId, string? userId);
        Task CheckIn(string confirmationCode, string userId);
        Task CancelReservation(Guid id);
        Task<CheckAvailabilityResponse> CheckAvailabilityReservation(CheckAvailabilityParams request);

        // Deposit
        Task<DepositStatusResponse> GetDepositStatus(Guid reservationId);
        Task<string> CreateDepositPaymentLink(Guid reservationId);
        Task ConfirmCashDeposit(Guid reservationId, string userId);
        Task AutoCancelDepositReservation(Guid reservationId);
        Task<byte[]> ExportAsync(ReservationFilterParams filter);
    }
}

using System.Linq.Expressions;
using AutoMapper;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Reservation;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.BLL.Interfaces.Reservations;
using RestX.BLL.Interfaces.Status;
using RestX.Models.Customers;
using RestX.Models.Enum;
using RestX.Models.Reservations;
using RestX.Models.Tables;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class ReservationService : BaseService, IReservationService
    {
        private const string ReservationStatusTypeCode = "RESERVATION";
        private const string PendingCode = "PENDING";
        private const string ConfirmedCode = "CONFIRMED";
        private const string CancelledCode = "CANCELLED";
        private const string CompletedCode = "COMPLETED";
        private const int ReservationBufferMinutes = 120;

        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static DateTime VnNow => DateTime.UtcNow.AddHours(7).Add(VietnamOffset);

        private const string ReservationIncludes = "ReservationTables.Table.Floor,Customer.ApplicationUser,ReservationStatus";
        private const string TablesIncludes = "ReservationTables.Table";
        private const string TablesAndStatusIncludes = "ReservationTables.Table,ReservationStatus";

        private readonly IMapper mapper;
        private readonly IStatusValueService statusValueService;
        private readonly IAuthService authService;
        private readonly IEmailService emailService;

        public ReservationService(
            IMapper mapper,
            IStatusValueService statusValueService,
            IAuthService authService,
            IEmailService emailService,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
            this.statusValueService = statusValueService;
            this.authService = authService;
            this.emailService = emailService;
        }

        public async Task<ReservationDetail> CreateReservation(CreateReservationRequest request)
        {
            ValidateFutureDate(request.ReservationDateTime);
            ValidateOperatingHours(request.ReservationDateTime);
            if (request.TableIds == null || request.TableIds.Count == 0)
                throw new ArgumentException("At least one table is required");
            ValidateDistinctTableIds(request.TableIds);

            var customerId = await ResolveCustomer(request.Phone, request.Name);

            var tables = await ValidateReservationTables(request.TableIds);
            await ValidateTableNotOccupied(tables, request.ReservationDateTime);
            ValidateCapacity(request.NumberOfGuests, tables);
            var availability = await CheckAvailabilityReservation(new CheckAvailabilityParams
            {
                TableIds = request.TableIds,
                ReservationDateTime = request.ReservationDateTime,
                BufferMinutes = ReservationBufferMinutes
            });
            if (!availability.Available)
                throw new InvalidOperationException("One or more tables are already reserved at this time");

            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var pendingStatus = statuses.FirstOrDefault(s => s.IsDefault)
                ?? throw new InvalidOperationException("Default reservation status not configured");

            var reservation = new Reservation
            {
                CustomerId = customerId,
                NumberOfGuests = request.NumberOfGuests,
                Time = request.ReservationDateTime,
                SpecialRequests = request.SpecialRequests,
                ReservationStatusId = pendingStatus.Id
            };

            await Repo.CreateAsync(reservation);
            reservation.ConfirmationCode = GenerateConfirmationCode(reservation.Id);
            Repo.Update(reservation);
            await Repo.SaveAsync();

            var saved = await LoadReservation(reservation.Id);
            var detail = mapper.Map<ReservationDetail>(saved!);
            await SendConfirmationEmail(request.Email, request.Name, detail, tables);
            return detail;
        }

        public async Task<PaginatedResult<ReservationListItem>> GetReservations(ReservationFilterParams filter)
        {
            var search = filter.Search?.ToLower();
            var predicate = BuildReservationFilter(filter, search);
            var totalCount = await Repo.GetCountAsync(predicate);
            var items = (await Repo.GetAsync<Reservation>(
                filter: predicate,
                orderBy: filter.SortDescending
                    ? q => q.OrderByDescending(r => r.Time)
                    : q => q.OrderBy(r =>
                                r.ReservationStatus.Code == CompletedCode || r.ReservationStatus.Code == CancelledCode ? 2 :
                                r.Time >= VnNow ? 0 : 1)
                            .ThenBy(r => r.Time)
                            .ThenBy(r => r.ReservationStatus.Code == ConfirmedCode ? 0 : r.ReservationStatus.Code == PendingCode ? 1 : 2),
                includeProperties: ReservationIncludes,
                skip: (filter.PageNumber - 1) * filter.PageSize,
                take: filter.PageSize
            )).ToList();

            return new PaginatedResult<ReservationListItem>(
                mapper.Map<IEnumerable<ReservationListItem>>(items),
                totalCount,
                filter.PageNumber,
                filter.PageSize);
        }

        public async Task<PaginatedResult<ReservationListItem>> GetMyReservations(Guid applicationUserId, PaginationParams pagination)
        {
            var customerId = await GetCustomerId(applicationUserId);
            var totalCount = await Repo.GetCountAsync<Reservation>(r => r.CustomerId == customerId);
            var items = (await Repo.GetAsync<Reservation>(
                filter: r => r.CustomerId == customerId,
                orderBy: q => q.OrderByDescending(r => r.Time),
                includeProperties: ReservationIncludes,
                skip: (pagination.PageNumber - 1) * pagination.PageSize,
                take: pagination.PageSize
            )).ToList();
            return new PaginatedResult<ReservationListItem>(
                mapper.Map<IEnumerable<ReservationListItem>>(items),
                totalCount,
                pagination.PageNumber,
                pagination.PageSize);
        }

        public async Task<ReservationDetail?> GetReservationById(Guid id)
        {
            var reservation = await LoadReservation(id);
            return reservation == null ? null : mapper.Map<ReservationDetail>(reservation);
        }

        public async Task<ReservationDetail?> GetReservationByCode(string confirmationCode)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.ConfirmationCode == confirmationCode,
                includeProperties: ReservationIncludes);
            return reservation == null ? null : mapper.Map<ReservationDetail>(reservation);
        }

        public async Task<ReservationDetail> UpdateReservation(Guid id, UpdateReservationRequest request)
        {
            var reservation = await RequireReservation(id, TablesAndStatusIncludes);

            var currentStatusCode = reservation.ReservationStatus?.Code;
            if (currentStatusCode == CancelledCode || currentStatusCode == CompletedCode)
                throw new InvalidOperationException("Cannot update a reservation that is already cancelled or completed");

            if (request.ReservationDateTime.HasValue)
            {
                ValidateFutureDate(request.ReservationDateTime.Value);
                ValidateOperatingHours(request.ReservationDateTime.Value);
            }

            if (request.TableIds is { Count: > 0 })
                ValidateDistinctTableIds(request.TableIds);

            var newDateTime = request.ReservationDateTime ?? reservation.Time;
            var tablesChanged = request.TableIds is { Count: > 0 };
            var dateChanged = request.ReservationDateTime.HasValue;
            //var newTableIds = tablesChanged ? request.TableIds! : currentTableIds;

            //List<Table> newTables = reservation.ReservationTables.Select(rt => rt.Table).ToList();
            //if (tablesChanged)
            //    newTables = await ValidateReservationTables(newTableIds);

            //if (tablesChanged || dateChanged)
            //    await ValidateTableNotOccupied(newTables, newDateTime);

            //if (tablesChanged || dateChanged)
            //{
            //    var conflicts = (await Repo.GetAsync<ReservationTable>(
            //        filter: rt =>
            //            newTableIds.Contains(rt.TableId) &&
            //            rt.ReservationId != id &&
            //            rt.Reservation.Time >= newDateTime.AddMinutes(-ReservationBufferMinutes) &&
            //            rt.Reservation.Time <= newDateTime.AddMinutes(ReservationBufferMinutes) &&
            //            rt.Reservation.ReservationStatus.Code != CancelledCode &&
            //            rt.Reservation.ReservationStatus.Code != CompletedCode,
            //        includeProperties: "Reservation.ReservationStatus"
            //    )).ToList();

            //    if (conflicts.Any())
            //        throw new InvalidOperationException("One or more tables are already reserved at this time");
            //}

            //var numberOfGuests = request.NumberOfGuests ?? reservation.NumberOfGuests;
            //ValidateCapacity(numberOfGuests, newTables);

            //if (tablesChanged)
            //{
            //    var removedTableIds = currentTableIds.Except(newTableIds).ToList();
            //    var addedTableIds = newTableIds.Except(currentTableIds).ToList();

            //    foreach (var rt in reservation.ReservationTables.Where(rt => removedTableIds.Contains(rt.TableId)).ToList())
            //    {
            //        Repo.Delete<ReservationTable>(rt.Id);
            //    }
            //    foreach (var table in newTables.Where(t => addedTableIds.Contains(t.Id)))
            //    {
            //        await Repo.CreateAsync(new ReservationTable { ReservationId = id, TableId = table.Id });
            //    }
            //}

            reservation.Time = newDateTime;
            //reservation.NumberOfGuests = numberOfGuests;
            reservation.SpecialRequests = request.SpecialRequests ?? reservation.SpecialRequests;

            Repo.Update(reservation);
            await Repo.SaveAsync();
            var saved = await LoadReservation(id);
            return mapper.Map<ReservationDetail>(saved!);
        }

        public async Task ChangeStatus(Guid id, int statusId, string? userId)
        {
            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var status = statuses.FirstOrDefault(s => s.Id == statusId)
                ?? throw new KeyNotFoundException($"Status ID {statusId} not found");

            if (status.Code.Equals(ConfirmedCode, StringComparison.OrdinalIgnoreCase))
                await ConfirmReservation(id, userId);
            else if (status.Code.Equals(CompletedCode, StringComparison.OrdinalIgnoreCase))
                await CompleteReservation(id, userId);
            else if (status.Code.Equals(CancelledCode, StringComparison.OrdinalIgnoreCase))
                await CancelReservation(id, userId);
            else
                throw new ArgumentException($"Cannot manually set status '{status.Code}'");
        }

        private async Task ConfirmReservation(Guid id, string? userId)
        {
            var reservation = await RequireReservation(id, TablesAndStatusIncludes);

            var statusCode = reservation.ReservationStatus?.Code;
            if (statusCode != PendingCode)
                throw new InvalidOperationException("Only pending reservations can be confirmed");

            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var confirmedStatus = statuses.FirstOrDefault(s => s.Code == ConfirmedCode)
                ?? throw new InvalidOperationException("Confirmed status not configured");

            reservation.ReservationStatusId = confirmedStatus.Id;
            Repo.Update(reservation, userId);
            await Repo.SaveAsync();
        }

        public async Task CheckIn(string confirmationCode, string userId)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.ConfirmationCode == confirmationCode,
                includeProperties: TablesAndStatusIncludes)
                ?? throw new KeyNotFoundException("Reservation not found");

            var statusCode = reservation.ReservationStatus?.Code;
            if (statusCode == CancelledCode || statusCode == CompletedCode)
                throw new InvalidOperationException("Cannot check in a reservation that is cancelled or completed");

            if (reservation.CheckedInAt.HasValue)
                throw new InvalidOperationException("Reservation has already been checked in");

            reservation.CheckedInAt = VnNow;
            Repo.Update(reservation, userId);

            await Repo.SaveAsync();
        }

        private async Task CompleteReservation(Guid id, string? userId)
        {
            var reservation = await RequireReservation(id, TablesIncludes);

            var statusCode = reservation.ReservationStatus?.Code;
            if (statusCode == CancelledCode || statusCode == CompletedCode)
                throw new InvalidOperationException("Reservation is already cancelled or completed");

            if (!reservation.CheckedInAt.HasValue)
                throw new InvalidOperationException("Cannot complete a reservation that has not been checked in");

            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var completedStatus = statuses.FirstOrDefault(s => s.Code == CompletedCode)
                ?? throw new InvalidOperationException("Completed status not configured");

            reservation.ReservationStatusId = completedStatus.Id;
            Repo.Update(reservation, userId);
            await FreeTablesAndSessions(reservation, CompletedCode, userId);
            await Repo.SaveAsync();
        }

        public async Task CancelReservation(Guid id)
        {
            await CancelReservation(id, null);
        }

        private async Task CancelReservation(Guid id, string? userId)
        {
            var reservation = await RequireReservation(id, TablesIncludes);
            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var cancelledStatus = statuses.FirstOrDefault(s => s.Code == CancelledCode)
                ?? throw new InvalidOperationException("Cancelled status not configured");
            reservation.ReservationStatusId = cancelledStatus.Id;
            Repo.Update(reservation, userId);
            await FreeTablesAndSessions(reservation, CancelledCode, userId);
            await Repo.SaveAsync();
        }

        public async Task<CheckAvailabilityResponse> CheckAvailabilityReservation(CheckAvailabilityParams request)
        {
            var bufferStart = request.ReservationDateTime.AddMinutes(-request.BufferMinutes);
            var bufferEnd = request.ReservationDateTime.AddMinutes(request.BufferMinutes);
            //var conflicts = (await Repo.GetAsync<ReservationTable>(
            //    filter: rt =>
            //        request.TableIds.Contains(rt.TableId) &&
            //        rt.Reservation.Time >= bufferStart &&
            //        rt.Reservation.Time <= bufferEnd &&
            //        rt.Reservation.ReservationStatus.Code != CancelledCode &&
            //        rt.Reservation.ReservationStatus.Code != CompletedCode,
            //    includeProperties: "Table,Reservation.ReservationStatus"
            //)).ToList();
            //var conflictingSlots = conflicts.Select(rt => new ConflictingSlot
            //{
            //    TableId = rt.TableId,
            //    TableCode = rt.Table.Code,
            //    ConflictTime = rt.Reservation.Time,
            //    ConflictStatus = rt.Reservation.ReservationStatus.Name
            //}).ToList();
            return new CheckAvailabilityResponse
            {
                //Available = !conflictingSlots.Any(),
                //ConflictingSlots = conflictingSlots
            };
        }

        #region Private Helpers
        private static Expression<Func<Reservation, bool>> BuildReservationFilter(ReservationFilterParams filter, string? search)
        {
            return r =>
                (!filter.StatusId.HasValue || r.ReservationStatusId == filter.StatusId.Value) &&
                (!filter.Date.HasValue || r.Time.Date == filter.Date.Value.Date) &&
                (!filter.TableId.HasValue) &&
                (string.IsNullOrEmpty(search) ||
                    (r.ConfirmationCode != null && r.ConfirmationCode.ToLower().Contains(search)) ||
                    (r.Customer.ApplicationUser.FullName != null &&
                     r.Customer.ApplicationUser.FullName.ToLower().Contains(search)));
        }

        private async Task<Guid?> GetCustomerId(Guid? applicationUserId)
        {
            if (applicationUserId == null)
                return null;
            var customer = await Repo.GetOneAsync<Customer>(
                filter: c => c.ApplicationUserId.Equals(applicationUserId));
            return customer?.Id;
        }

        private async Task SendConfirmationEmail(
            string email, string name, ReservationDetail detail, List<Table> tables)
        {
            try
            {
                var tableList = string.Join(", ", tables.Select(t => t.Code));
                var body = EmailTemplates.ReservationConfirmation(
                    name,
                    detail.ConfirmationCode,
                    detail.ReservationDateTime,
                    detail.NumberOfGuests,
                    tableList,
                    detail.SpecialRequests);
                await emailService.SendEmailAsync(email, "Reservation Confirmation – " + detail.ConfirmationCode, body);
            }
            catch
            {
            }
        }

        private async Task<List<Table>> ValidateReservationTables(List<Guid> tableIds)
        {
            var tables = (await Repo.GetAsync<Table>(
                filter: t => tableIds.Contains(t.Id) && t.IsActive,
                includeProperties: "Floor")).ToList();

            var foundIds = tables.Select(t => t.Id).ToHashSet();
            var missingId = tableIds.FirstOrDefault(id => !foundIds.Contains(id));
            if (missingId != default)
                throw new KeyNotFoundException($"Table not found: {missingId}");

            return tables;
        }

        private async Task ValidateTableNotOccupied(List<Table> tables, DateTime reservationDateTime)
        {
            var occupiedTables = tables.Where(t => t.TableStatusId == TableStatus.Occupied).ToList();
            if (!occupiedTables.Any()) return;

            var occupiedTableIds = occupiedTables.Select(t => t.Id).ToList();
            var activeSessions = (await Repo.GetAsync<TableSession>(
                filter: ts => occupiedTableIds.Contains(ts.TableId) && ts.IsActive
            )).ToList();

            foreach (var table in occupiedTables)
            {
                var session = activeSessions.FirstOrDefault(s => s.TableId == table.Id);
                var estimatedEnd = session != null
                    ? session.StartedAt.AddMinutes(ReservationBufferMinutes)
                    : VnNow.AddMinutes(ReservationBufferMinutes);

                if (reservationDateTime < estimatedEnd)
                    throw new InvalidOperationException(
                        $"Table '{table.Code}' is currently occupied. Estimated available after {estimatedEnd:HH:mm}");
            }
        }

        private static readonly Dictionary<string, int> DayOrder = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Mon"] = 0,
            ["Tue"] = 1,
            ["Wed"] = 2,
            ["Thu"] = 3,
            ["Fri"] = 4,
            ["Sat"] = 5,
            ["Sun"] = 6
        };

        private void ValidateOperatingHours(DateTime reservationDateTime)
        {
            var openingHours = CurrentTenant?.BusinessOpeningHours;
            if (string.IsNullOrWhiteSpace(openingHours)) return;

            var localDateTime = reservationDateTime.Kind == DateTimeKind.Utc
                ? reservationDateTime.Add(VietnamOffset)
                : reservationDateTime;

            var segments = openingHours.Split(',');
            var hasValidSegment = false;
            var dayMatched = false;

            foreach (var segment in segments)
            {
                var parts = segment.Trim().Split(new[] { ": " }, 2, StringSplitOptions.None);
                if (parts.Length != 2) continue;

                var timeParts = parts[1].Trim().Split('-');
                if (timeParts.Length != 2) continue;
                if (!TimeSpan.TryParse(timeParts[0].Trim(), out var openTime)) continue;
                if (!TimeSpan.TryParse(timeParts[1].Trim(), out var closeTime)) continue;

                hasValidSegment = true;

                if (!IsDayInRange(localDateTime.DayOfWeek, parts[0].Trim())) continue;

                dayMatched = true;
                var reservationTime = localDateTime.TimeOfDay;
                if (reservationTime < openTime || reservationTime >= closeTime)
                    throw new ArgumentException(
                        $"Reservation time must be between {openTime:hh\\:mm} and {closeTime:hh\\:mm}");
                return;
            }

            if (hasValidSegment && !dayMatched)
                throw new ArgumentException(
                    $"Restaurant is closed on {localDateTime.DayOfWeek}");
        }

        private static bool IsDayInRange(DayOfWeek day, string dayRange)
        {
            if (!DayOrder.TryGetValue(day.ToString()[..3], out var dayOrder)) return false;

            var rangeParts = dayRange.Split('-');
            if (rangeParts.Length == 1)
                return DayOrder.TryGetValue(rangeParts[0].Trim(), out var single) && single == dayOrder;

            if (rangeParts.Length == 2)
            {
                if (!DayOrder.TryGetValue(rangeParts[0].Trim(), out var start)) return false;
                if (!DayOrder.TryGetValue(rangeParts[1].Trim(), out var end)) return false;
                return dayOrder >= start && dayOrder <= end;
            }
            return false;
        }

        private static void ValidateFutureDate(DateTime dateTime)
        {
            var localDateTime = dateTime.Kind == DateTimeKind.Utc
                ? dateTime.Add(VietnamOffset)
                : dateTime;
            if (localDateTime <= VnNow)
                throw new ArgumentException("Reservation date and time must be in the future");
            if (localDateTime > VnNow.AddMonths(1))
                throw new ArgumentException("Reservation can only be made up to 1 month in advance");
        }

        private static void ValidateDistinctTableIds(List<Guid> tableIds)
        {
            if (tableIds.Distinct().Count() != tableIds.Count)
                throw new ArgumentException("Duplicate table IDs are not allowed");
        }

        private static void ValidateCapacity(int numberOfGuests, List<Table> tables)
        {
            var totalCapacity = tables.Sum(t => t.SeatingCapacity);
            if (numberOfGuests > totalCapacity)
                throw new InvalidOperationException(
                    $"Number of guests ({numberOfGuests}) exceeds total table capacity ({totalCapacity})");
        }

        private async Task FreeTablesAndSessions(Reservation reservation, string newStatusCode, string? userId = null)
        {
            if (newStatusCode != CompletedCode && newStatusCode != CancelledCode)
                return;

            var activeSessions = (await Repo.GetAsync<TableSession>(
                filter: ts => ts.ReservationId == reservation.Id && ts.IsActive)).ToList();
            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.EndedAt = VnNow;
                Repo.Update(session, userId);
            }
        }

        private async Task<Reservation> RequireReservation(Guid id, string includes)
        {
            return await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == id,
                includeProperties: includes)
                ?? throw new KeyNotFoundException("Reservation not found");
        }

        private async Task<Reservation?> LoadReservation(Guid id)
        {
            return await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == id,
                includeProperties: ReservationIncludes);
        }
        private async Task<Guid> ResolveCustomer(string phone, string name)
        {
            var existing = await authService.CheckPhoneNumberAsync(phone);
            if (existing.Exists && existing.CustomerId.HasValue)
                return existing.CustomerId.Value;

            var register = await authService.CustomerPhoneRegisterAsync(new CustomerPhoneRegisterRequest
            {
                PhoneNumber = phone,
                FullName = name
            });
            if (!register.Success)
                throw new InvalidOperationException($"Failed to register customer: {register.Message}");

            var lookup = await authService.CheckPhoneNumberAsync(phone);
            return lookup.CustomerId!.Value;
        }

        private static string GenerateConfirmationCode(Guid id)
            => id.ToString("N")[..6].ToUpper();

        #endregion
    }
}
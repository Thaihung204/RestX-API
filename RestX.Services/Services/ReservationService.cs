using System.Linq.Expressions;
using AutoMapper;
using Hangfire;
using PayOS;
using PayOS.Models.Webhooks;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Reservation;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.BLL.Interfaces.Reservations;
using RestX.BLL.Interfaces.Status;
using RestX.Models.Customers;
using RestX.Models.Enum;
using RestX.Models.Orders;
using RestX.Models.Reservations;
using RestX.Models.Tables;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class ReservationService : BaseService, IReservationService
    {
        private const string ReservationStatusTypeCode = "RESERVATION";
        private const string DepositPendingCode = "DEPOSIT_PENDING";
        private const string ConfirmedCode = "CONFIRMED";
        private const string CancelledCode = "CANCELLED";
        private const string CompletedCode = "COMPLETED";
        private const string NoShowCode = "NO_SHOW";
        private const int ReservationBufferMinutes = 120;

        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static DateTime VnNow => DateTime.UtcNow.Add(VietnamOffset);

        private const string ReservationIncludes = "TableSessions.Table.Floor,Customer.ApplicationUser,ReservationStatus";
        private const string TablesIncludes = "TableSessions.Table";
        private const string TablesAndStatusIncludes = "TableSessions.Table,ReservationStatus";

        private readonly IMapper mapper;
        private readonly IStatusValueService statusValueService;
        private readonly IAuthService authService;
        private readonly IEmailService emailService;
        private readonly IDepositConfigService depositConfigService;
        private readonly IPaymentSettingService paymentSettingService;

        public ReservationService(
            IMapper mapper,
            IStatusValueService statusValueService,
            IAuthService authService,
            IEmailService emailService,
            IDepositConfigService depositConfigService,
            IPaymentSettingService paymentSettingService,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
            this.statusValueService = statusValueService;
            this.authService = authService;
            this.emailService = emailService;
            this.depositConfigService = depositConfigService;
            this.paymentSettingService = paymentSettingService;
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

            var depositConfig = await depositConfigService.GetDepositConfig(CurrentTenant.Id);
            var requiresDeposit = depositConfig != null && request.NumberOfGuests >= depositConfig.MinPartySize;

            string initialStatusCode;
            decimal depositAmount = 0;
            DateTime? paymentDeadline = null;

            if (requiresDeposit)
            {
                initialStatusCode = DepositPendingCode;
                depositAmount = request.NumberOfGuests * depositConfig!.DepositAmountPerPerson;
                paymentDeadline = VnNow.AddHours(depositConfig.DeadlineHours);
            }
            else
            {
                initialStatusCode = ConfirmedCode;
            }

            var initialStatus = statuses.FirstOrDefault(s => s.Code == initialStatusCode)
                ?? throw new InvalidOperationException($"Status '{initialStatusCode}' not configured");

            var reservation = new Reservation
            {
                CustomerId = customerId,
                NumberOfGuests = request.NumberOfGuests,
                Time = request.ReservationDateTime,
                SpecialRequests = request.SpecialRequests,
                ReservationStatusId = initialStatus.Id,
                DepositAmount = depositAmount,
                PaymentDeadline = paymentDeadline
            };

            await Repo.CreateAsync(reservation);
            reservation.ConfirmationCode = GenerateConfirmationCode(reservation.Id);
            Repo.Update(reservation);

            var order = new Order
            {
                Reference = GenerateOrderReference(reservation.Id),
                CustomerId = customerId,
                ReservationId = reservation.Id,
                OrderStatusId = OrderStatus.Pending
            };
            await Repo.CreateAsync(order);

            foreach (var table in tables)
            {
                await Repo.CreateAsync(new TableSession
                {
                    TableId = table.Id,
                    ReservationId = reservation.Id,
                    CurrentOrderId = order.Id,
                    StartedAt = request.ReservationDateTime,
                    IsActive = true
                });
            }
            await Repo.SaveAsync();

            if (requiresDeposit)
                BackgroundJob.Schedule<IReservationService>(
                    s => s.AutoCancelDepositReservation(reservation.Id),
                    paymentDeadline!.Value);

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
                            .ThenBy(r => r.ReservationStatus.Code == ConfirmedCode ? 0 : r.ReservationStatus.Code == DepositPendingCode ? 1 : 2),
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
            var currentSessions = reservation.TableSessions.ToList();
            var currentTableIds = currentSessions.Select(ts => ts.TableId).ToList();
            var tablesChanged = request.TableIds is { Count: > 0 };
            var dateChanged = request.ReservationDateTime.HasValue;
            var newTableIds = tablesChanged ? request.TableIds! : currentTableIds;

            List<Table> newTables = currentSessions.Select(ts => ts.Table).ToList();
            if (tablesChanged)
                newTables = await ValidateReservationTables(newTableIds);

            if (tablesChanged || dateChanged)
                await ValidateTableNotOccupied(newTables, newDateTime);

            if (tablesChanged || dateChanged)
            {
                var conflicts = (await Repo.GetAsync<TableSession>(
                    filter: ts =>
                        newTableIds.Contains(ts.TableId) &&
                        ts.ReservationId != id &&
                        ts.ReservationId != null &&
                        ts.Reservation.Time >= newDateTime.AddMinutes(-ReservationBufferMinutes) &&
                        ts.Reservation.Time <= newDateTime.AddMinutes(ReservationBufferMinutes) &&
                        ts.Reservation.ReservationStatus.Code != CancelledCode &&
                        ts.Reservation.ReservationStatus.Code != CompletedCode,
                    includeProperties: "Reservation.ReservationStatus"
                )).ToList();

                if (conflicts.Any())
                    throw new InvalidOperationException("One or more tables are already reserved at this time");
            }

            var numberOfGuests = request.NumberOfGuests ?? reservation.NumberOfGuests;
            ValidateCapacity(numberOfGuests, newTables);

            if (tablesChanged)
            {
                var removedTableIds = currentTableIds.Except(newTableIds).ToList();
                var addedTableIds = newTableIds.Except(currentTableIds).ToList();

                var sharedOrderId = currentSessions.FirstOrDefault()?.CurrentOrderId;

                foreach (var session in currentSessions.Where(ts => removedTableIds.Contains(ts.TableId)))
                {
                    Repo.Delete<TableSession>(session.Id);
                }
                foreach (var table in newTables.Where(t => addedTableIds.Contains(t.Id)))
                {
                    await Repo.CreateAsync(new TableSession
                    {
                        TableId = table.Id,
                        ReservationId = id,
                        CurrentOrderId = sharedOrderId,
                        StartedAt = newDateTime,
                        IsActive = true
                    });
                }
            }
            else if (dateChanged)
            {
                if (!reservation.CheckedInAt.HasValue)
                {
                    foreach (var session in currentSessions)
                    {
                        session.StartedAt = newDateTime;
                        Repo.Update(session);
                    }
                }
            }

            reservation.Time = newDateTime;
            reservation.NumberOfGuests = numberOfGuests;
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

            if (status.Code.Equals(CancelledCode, StringComparison.OrdinalIgnoreCase))
                await CancelReservation(id, userId);
            else if (status.Code.Equals(NoShowCode, StringComparison.OrdinalIgnoreCase))
                await NoShowReservation(id, userId);
            else
                throw new ArgumentException($"Cannot manually set status '{status.Code}'");
        }

        private async Task ConfirmReservation(Guid id, string? userId)
        {
            var reservation = await RequireReservation(id, TablesAndStatusIncludes);

            var statusCode = reservation.ReservationStatus?.Code;
            if (statusCode != DepositPendingCode)
                throw new InvalidOperationException("Only deposit-pending reservations can be confirmed this way");

            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var confirmedStatus = statuses.FirstOrDefault(s => s.Code == ConfirmedCode)
                ?? throw new InvalidOperationException("Confirmed status not configured");

            reservation.ReservationStatusId = confirmedStatus.Id;
            Repo.Update(reservation, userId);
            await Repo.SaveAsync();
        }

        private async Task NoShowReservation(Guid id, string? userId)
        {
            var reservation = await RequireReservation(id, TablesIncludes);
            var statusCode = reservation.ReservationStatus?.Code;
            if (statusCode != ConfirmedCode)
                throw new InvalidOperationException("Only confirmed reservations can be marked as no-show");

            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var noShowStatus = statuses.FirstOrDefault(s => s.Code == NoShowCode)
                ?? throw new InvalidOperationException("No-show status not configured");

            reservation.ReservationStatusId = noShowStatus.Id;
            Repo.Update(reservation, userId);
            await FreeTablesAndSessions(reservation, NoShowCode, userId);
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
            if (statusCode == DepositPendingCode)
                throw new InvalidOperationException("Cannot check in a reservation that has an unpaid deposit");

            if (reservation.CheckedInAt.HasValue)
                throw new InvalidOperationException("Reservation has already been checked in");

            reservation.CheckedInAt = VnNow;
            Repo.Update(reservation, userId);

            var sessions = (await Repo.GetAsync<TableSession>(
                filter: ts => ts.ReservationId == reservation.Id && ts.IsActive,
                includeProperties: "Table")).ToList();

            foreach (var session in sessions)
            {
                session.StartedAt = VnNow;
                session.Table.TableStatusId = TableStatus.Occupied;
                Repo.Update(session, userId);
                Repo.Update(session.Table, userId);
            }

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
            var reservation = await RequireReservation(id, TablesAndStatusIncludes);
            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var cancelledStatus = statuses.FirstOrDefault(s => s.Code == CancelledCode)
                ?? throw new InvalidOperationException("Cancelled status not configured");
            if (reservation.ReservationStatus?.Code == DepositPendingCode)
            {
                var pendingPayment = await Repo.GetOneAsync<Payment>(
                    p => p.ReservationId == id && p.Purpose == PaymentPurpose.Deposit && p.Status == PaymentStatus.Pending);
                if (pendingPayment?.PayOSOrderCode != null)
                {
                    try
                    {
                        var (gatewayClient, _) = await GetDepositGateway();
                        await gatewayClient.PaymentRequests.CancelAsync(pendingPayment.PayOSOrderCode.Value, "Reservation cancelled");
                    }
                    catch { }
                    pendingPayment.Status = PaymentStatus.Fail;
                    Repo.Update(pendingPayment);
                }
            }

            reservation.ReservationStatusId = cancelledStatus.Id;
            Repo.Update(reservation, userId);
            await FreeTablesAndSessions(reservation, CancelledCode, userId);
            await Repo.SaveAsync();
        }

        public async Task<CheckAvailabilityResponse> CheckAvailabilityReservation(CheckAvailabilityParams request)
        {
            var bufferStart = request.ReservationDateTime.AddMinutes(-request.BufferMinutes);
            var bufferEnd = request.ReservationDateTime.AddMinutes(request.BufferMinutes);
            var conflicts = (await Repo.GetAsync<TableSession>(
                filter: ts =>
                    request.TableIds.Contains(ts.TableId) &&
                    ts.ReservationId != null &&
                    ts.Reservation.Time >= bufferStart &&
                    ts.Reservation.Time <= bufferEnd &&
                    ts.Reservation.ReservationStatus.Code != CancelledCode &&
                    ts.Reservation.ReservationStatus.Code != CompletedCode,
                includeProperties: "Table,Reservation.ReservationStatus"
            )).ToList();
            var conflictingSlots = conflicts.Select(ts => new ConflictingSlot
            {
                TableId = ts.TableId,
                TableCode = ts.Table.Code,
                ConflictTime = ts.Reservation!.Time,
                ConflictStatus = ts.Reservation.ReservationStatus.Name
            }).ToList();
            return new CheckAvailabilityResponse
            {
                Available = !conflictingSlots.Any(),
                ConflictingSlots = conflictingSlots
            };
        }

        #region Private Helpers
        private static Expression<Func<Reservation, bool>> BuildReservationFilter(ReservationFilterParams filter, string? search)
        {
            return r =>
                (!filter.StatusId.HasValue || r.ReservationStatusId == filter.StatusId.Value) &&
                (!filter.Date.HasValue || r.Time.Date == filter.Date.Value.Date) &&
                (!filter.TableId.HasValue || r.TableSessions.Any(ts => ts.TableId == filter.TableId.Value)) &&
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

            var sessions = (await Repo.GetAsync<TableSession>(
                filter: ts => ts.ReservationId == reservation.Id && ts.IsActive,
                includeProperties: "Table")).ToList();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.EndedAt = VnNow;
                session.Table.TableStatusId = TableStatus.Available;
                Repo.Update(session, userId);
                Repo.Update(session.Table, userId);
            }

            // Huỷ order rỗng (chưa có detail) nếu là cancel
            if (newStatusCode == CancelledCode)
            {
                var sharedOrderId = sessions.FirstOrDefault()?.CurrentOrderId;
                if (sharedOrderId.HasValue)
                {
                    var order = await Repo.GetOneAsync<Order>(
                        filter: o => o.Id == sharedOrderId.Value,
                        includeProperties: "OrderDetails");
                    if (order != null && !order.OrderDetails.Any())
                    {
                        order.OrderStatusId = OrderStatus.Cancelled;
                        order.CancelledAt = VnNow;
                        Repo.Update(order, userId);
                    }
                }
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

        private static string GenerateOrderReference(Guid reservationId)
            => "R-" + reservationId.ToString("N")[..8].ToUpper();

        #endregion

        #region Deposit

        public async Task<DepositStatusResponse> GetDepositStatus(Guid reservationId)
        {
            var reservation = await Repo.GetByIdAsync<Reservation>(reservationId)
                ?? throw new KeyNotFoundException("Reservation not found");

            var depositPayments = await Repo.GetAsync<Payment>(
                p => p.ReservationId == reservationId && p.Purpose == PaymentPurpose.Deposit,
                orderBy: q => q.OrderByDescending(p => p.PaymentDate));
            var depositPayment = depositPayments.FirstOrDefault();

            return new DepositStatusResponse
            {
                ReservationId = reservationId,
                DepositAmount = reservation.DepositAmount,
                PaymentDeadline = reservation.PaymentDeadline,
                IsPaid = depositPayment?.Status == PaymentStatus.Success,
                CheckoutUrl = depositPayment?.Status == PaymentStatus.Pending ? depositPayment.CheckoutUrl : null,
                PaymentStatus = depositPayment?.Status
            };
        }

        public async Task<string> CreateDepositPaymentLink(Guid reservationId)
        {
            var reservation = await Repo.GetByIdAsync<Reservation>(reservationId)
                ?? throw new KeyNotFoundException("Reservation not found");

            if (reservation.ReservationStatus?.Code != DepositPendingCode)
            {
                var status = await Repo.GetOneAsync<RestX.Models.Common.StatusValue>(s => s.Id == reservation.ReservationStatusId);
                if (status?.Code != DepositPendingCode)
                    throw new InvalidOperationException("Reservation is not in deposit-pending status");
            }

            var alreadyPaid = await Repo.GetExistsAsync<Payment>(
                p => p.ReservationId == reservationId && p.Purpose == PaymentPurpose.Deposit && p.Status == PaymentStatus.Success);
            if (alreadyPaid)
                throw new InvalidOperationException("Deposit has already been paid");

            // Cancel existing unpaid link if any
            var existingUnpaid = await Repo.GetOneAsync<Payment>(
                p => p.ReservationId == reservationId && p.Purpose == PaymentPurpose.Deposit && p.Status == PaymentStatus.Pending);
            if (existingUnpaid?.PayOSOrderCode != null)
            {
                var (gatewayClient, _) = await GetDepositGateway();
                await gatewayClient.PaymentRequests.CancelAsync(existingUnpaid.PayOSOrderCode.Value, "Recreating deposit link");
                existingUnpaid.Status = PaymentStatus.Fail;
                Repo.Update(existingUnpaid);
                await Repo.SaveAsync();
            }

            var (client, settings) = await GetDepositGateway();
            var orderCode = GenerateDepositOrderCode();
            var description = $"Coc dat ban {reservation.ConfirmationCode}";
            if (description.Length > 25) description = description[..25];

            var linkRequest = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (long)reservation.DepositAmount,
                Description = description,
                Items = new List<PayOS.Models.V2.PaymentRequests.PaymentLinkItem>
                {
                    new() { Name = "Tien coc dat ban", Quantity = 1, Price = (long)reservation.DepositAmount }
                },
                ReturnUrl = settings.ReturnUrl,
                CancelUrl = settings.CancelUrl
            };

            var link = await client.PaymentRequests.CreateAsync(linkRequest);

            var payment = new Payment
            {
                ReservationId = reservationId,
                PaymentMethodId = "BANK",
                Amount = reservation.DepositAmount,
                PayOSOrderCode = orderCode,
                CheckoutUrl = link.CheckoutUrl,
                Status = PaymentStatus.Pending,
                Purpose = PaymentPurpose.Deposit,
                PaymentDate = VnNow
            };

            await Repo.CreateAsync(payment);
            await Repo.SaveAsync();

            return link.CheckoutUrl;
        }

        public async Task ConfirmCashDeposit(Guid reservationId, string userId)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == reservationId,
                includeProperties: "ReservationStatus")
                ?? throw new KeyNotFoundException("Reservation not found");

            if (reservation.ReservationStatus?.Code != DepositPendingCode)
                throw new InvalidOperationException("Reservation is not in deposit-pending status");

            var alreadyPaid = await Repo.GetExistsAsync<Payment>(
                p => p.ReservationId == reservationId && p.Purpose == PaymentPurpose.Deposit && p.Status == PaymentStatus.Success);
            if (alreadyPaid)
                throw new InvalidOperationException("Deposit has already been paid");

            var payment = new Payment
            {
                ReservationId = reservationId,
                PaymentMethodId = "CASH",
                Amount = reservation.DepositAmount,
                Status = PaymentStatus.Success,
                Purpose = PaymentPurpose.Deposit,
                PaymentDate = VnNow,
                ProcessedBy = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId)
            };

            await Repo.CreateAsync(payment, userId);
            await ConfirmReservation(reservationId, userId);
            await Repo.SaveAsync();
        }


        public async Task AutoCancelDepositReservation(Guid reservationId)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == reservationId,
                includeProperties: "ReservationStatus")
                ?? throw new KeyNotFoundException("Reservation not found");

            if (reservation.ReservationStatus?.Code != DepositPendingCode)
                return;

            var hasPaidDeposit = await Repo.GetExistsAsync<Payment>(
                p => p.ReservationId == reservationId && p.Purpose == PaymentPurpose.Deposit && p.Status == PaymentStatus.Success);
            if (hasPaidDeposit)
                return;

            await CancelReservation(reservationId, null);
        }

        private async Task<(PayOSClient client, DataTranferObjects.Common.PaymentGatewaySettings settings)> GetDepositGateway()
        {
            var settings = await paymentSettingService.GetPaymentSettingByTenantId(CurrentTenant.Id)
                ?? throw new InvalidOperationException("Payment gateway is not configured for this tenant");
            return (new PayOSClient(settings.ClientId, settings.ApiKey, settings.ChecksumKey, null), settings);
        }

        private static long GenerateDepositOrderCode()
        {
            var timestamp = DateTimeOffset.UtcNow.ToOffset(VietnamOffset).ToUnixTimeSeconds();
            var suffix = Random.Shared.Next(1000, 9999);
            return long.Parse($"9{timestamp}{suffix}");
        }

        #endregion
    }
}
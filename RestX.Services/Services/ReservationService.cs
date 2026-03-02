using System.Linq.Expressions;
using AutoMapper;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Reservation;
using RestX.BLL.DataTranferObjects.Status;
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
        private const string CancelledCode = "CANCELLED";
        private const string CompletedCode = "COMPLETED";

        private const string CustomerRole = "Customer";

        private readonly IMapper mapper;
        private readonly IStatusValueService statusValueService;
        private readonly IUserAccountService userAccountService;
        private readonly IEmailService emailService;

        public ReservationService(
            IMapper mapper,
            IStatusValueService statusValueService,
            IUserAccountService userAccountService,
            IEmailService emailService,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
            this.statusValueService = statusValueService;
            this.userAccountService = userAccountService;
            this.emailService = emailService;
        }

        public async Task<ReservationDetail> CreateReservation(CreateReservationRequest request)
        {
            if (request.ReservationDateTime <= DateTime.UtcNow)
                throw new ArgumentException("Reservation date and time must be in the future");
            if (request.TableIds == null || request.TableIds.Count == 0)
                throw new ArgumentException("At least one table is required");
            if (request.TableIds.Distinct().Count() != request.TableIds.Count)
                throw new ArgumentException("Duplicate table IDs are not allowed");

            var customerId = await ResolveOrCreateCustomer(request.Phone, request.Name, request.Email);

            var tables = await GetAndValidateTables(request.TableIds);
            ValidateCapacity(request.NumberOfGuests, tables);
            var availability = await CheckAvailabilityReservation(new CheckAvailabilityParams
            {
                TableIds = request.TableIds,
                ReservationDateTime = request.ReservationDateTime
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
            foreach (var table in tables)
            {
                await Repo.CreateAsync(new ReservationTable
                {
                    ReservationId = reservation.Id,
                    TableId = table.Id
                });
                table.TableStatusId = TableStatus.Reserved;
                Repo.Update(table);
            }
            await Repo.SaveAsync();

            var created = await GetReservationWithInfo(reservation.Id);
            var result = mapper.Map<ReservationDetail>(created!);
            await TrySendReservationConfirmationAsync(request.Email, request.Name, result, tables);
            return result;
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
                    : q => q.OrderBy(r => r.Time),
                includeProperties: "ReservationTables.Table.Floor,Customer.ApplicationUser,ReservationStatus",
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
            var customerId = await ResolveCustomerId(applicationUserId);
            var totalCount = await Repo.GetCountAsync<Reservation>(r => r.CustomerId == customerId);
            var items = (await Repo.GetAsync<Reservation>(
                filter: r => r.CustomerId == customerId,
                orderBy: q => q.OrderByDescending(r => r.Time),
                includeProperties: "ReservationTables.Table.Floor,Customer.ApplicationUser,ReservationStatus",
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
            var reservation = await GetReservationWithInfo(id);
            return reservation == null ? null : mapper.Map<ReservationDetail>(reservation);
        }

        public async Task<ReservationDetail?> GetReservationByCode(string confirmationCode)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.ConfirmationCode == confirmationCode,
                includeProperties: "ReservationTables.Table.Floor,Customer.ApplicationUser,ReservationStatus");
            return reservation == null ? null : mapper.Map<ReservationDetail>(reservation);
        }

        public async Task<ReservationDetail> UpdateReservation(Guid id, UpdateReservationRequest request)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == id,
                includeProperties: "ReservationTables.Table,ReservationStatus");
            if (reservation == null)
                throw new KeyNotFoundException("Reservation not found");

            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var currentStatus = statuses.FirstOrDefault(s => s.Id == reservation.ReservationStatusId);
            if (currentStatus?.Code == CancelledCode || currentStatus?.Code == CompletedCode)
                throw new InvalidOperationException("Cannot update a reservation that is already cancelled or completed");

            if (request.ReservationDateTime.HasValue && request.ReservationDateTime.Value <= DateTime.UtcNow)
                throw new ArgumentException("Reservation date and time must be in the future");

            if (request.TableIds != null && request.TableIds.Count > 0 &&
                request.TableIds.Distinct().Count() != request.TableIds.Count)
                throw new ArgumentException("Duplicate table IDs are not allowed");

            var newDateTime = request.ReservationDateTime ?? reservation.Time;
            var currentTableIds = reservation.ReservationTables.Select(rt => rt.TableId).ToList();
            var newTableIds = request.TableIds != null && request.TableIds.Count > 0
                ? request.TableIds
                : currentTableIds;

            var tablesChanged = request.TableIds != null && request.TableIds.Count > 0;
            var dateChanged = request.ReservationDateTime.HasValue;

            List<Table> newTables = reservation.ReservationTables.Select(rt => rt.Table).ToList();
            if (tablesChanged)
                newTables = await GetAndValidateTables(newTableIds);

            if (tablesChanged || dateChanged)
            {
                var conflicts = (await Repo.GetAsync<ReservationTable>(
                    filter: rt =>
                        newTableIds.Contains(rt.TableId) &&
                        rt.ReservationId != id &&
                        rt.Reservation.Time >= newDateTime.AddMinutes(-120) &&
                        rt.Reservation.Time <= newDateTime.AddMinutes(120) &&
                        rt.Reservation.ReservationStatus.Code != CancelledCode &&
                        rt.Reservation.ReservationStatus.Code != CompletedCode,
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

                foreach (var rt in reservation.ReservationTables.Where(rt => removedTableIds.Contains(rt.TableId)).ToList())
                {
                    rt.Table.TableStatusId = TableStatus.Available;
                    Repo.Update(rt.Table);
                    Repo.Delete<ReservationTable>(rt.Id);
                }
                foreach (var table in newTables.Where(t => addedTableIds.Contains(t.Id)))
                {
                    await Repo.CreateAsync(new ReservationTable { ReservationId = id, TableId = table.Id });
                    table.TableStatusId = TableStatus.Reserved;
                    Repo.Update(table);
                }
            }

            reservation.Time = newDateTime;
            reservation.NumberOfGuests = numberOfGuests;
            reservation.SpecialRequests = request.SpecialRequests ?? reservation.SpecialRequests;

            Repo.Update(reservation);
            await Repo.SaveAsync();
            var updated = await GetReservationWithInfo(id);
            return mapper.Map<ReservationDetail>(updated!);
        }

        public async Task UpdateReservationStatus(Guid id, int statusId)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == id,
                includeProperties: "ReservationTables.Table");
            if (reservation == null)
                throw new KeyNotFoundException("Reservation not found");
            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var status = statuses.FirstOrDefault(s => s.Id == statusId)
                ?? throw new InvalidOperationException("Invalid reservation status");
            reservation.ReservationStatusId = statusId;
            Repo.Update(reservation);
            await ApplyTableStatusSideEffect(reservation, status.Code);
            await Repo.SaveAsync();
        }

        public async Task CancelReservation(Guid id)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == id,
                includeProperties: "ReservationTables.Table");
            if (reservation == null)
                throw new KeyNotFoundException("Reservation not found");
            var statuses = await statusValueService.GetStatuses(ReservationStatusTypeCode);
            var cancelledStatus = statuses.FirstOrDefault(s => s.Code == CancelledCode)
                ?? throw new InvalidOperationException("Cancelled status not configured");
            reservation.ReservationStatusId = cancelledStatus.Id;
            Repo.Update(reservation);
            foreach (var rt in reservation.ReservationTables)
            {
                rt.Table.TableStatusId = TableStatus.Available;
                Repo.Update(rt.Table);
            }
            await Repo.SaveAsync();
        }

        public async Task<CheckAvailabilityResponse> CheckAvailabilityReservation(CheckAvailabilityParams request)
        {
            var bufferStart = request.ReservationDateTime.AddMinutes(-request.BufferMinutes);
            var bufferEnd = request.ReservationDateTime.AddMinutes(request.BufferMinutes);
            var conflicts = (await Repo.GetAsync<ReservationTable>(
                filter: rt =>
                    request.TableIds.Contains(rt.TableId) &&
                    rt.Reservation.Time >= bufferStart &&
                    rt.Reservation.Time <= bufferEnd &&
                    rt.Reservation.ReservationStatus.Code != CancelledCode &&
                    rt.Reservation.ReservationStatus.Code != CompletedCode,
                includeProperties: "Table,Reservation.ReservationStatus"
            )).ToList();
            var conflictingSlots = conflicts.Select(rt => new ConflictingSlot
            {
                TableId = rt.TableId,
                TableCode = rt.Table.Code,
                ConflictTime = rt.Reservation.Time,
                ConflictStatus = rt.Reservation.ReservationStatus.Name
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
                (!filter.TableId.HasValue || r.ReservationTables.Any(rt => rt.TableId == filter.TableId.Value)) &&
                (string.IsNullOrEmpty(search) ||
                    (r.ConfirmationCode != null && r.ConfirmationCode.ToLower().Contains(search)) ||
                    (r.Customer != null && r.Customer.ApplicationUser.UserName != null &&
                     r.Customer.ApplicationUser.UserName.ToLower().Contains(search)));
        }

        private async Task<Guid?> ResolveCustomerId(Guid? applicationUserId)
        {
            if (!applicationUserId.HasValue)
                return null;
            var customer = await Repo.GetOneAsync<Customer>(
                filter: c => c.ApplicationUserId == applicationUserId.Value);
            return customer?.Id;
        }

        private async Task<Guid> ResolveOrCreateCustomer(string phone, string name, string email)
        {
            var existing = await Repo.GetOneAsync<Customer>(
                filter: c => c.ApplicationUser.PhoneNumber == phone,
                includeProperties: "ApplicationUser");
            if (existing != null)
                return existing.Id;

            var user = await userAccountService.CreateUserAsync(new CreateUserRequest
            {
                FullName = name,
                Email = email,
                PhoneNumber = phone,
                Role = CustomerRole,
                GenerateRandomPassword = true
            });
            var customer = new Customer
            {
                ApplicationUserId = user.Id,
                MembershipLevel = "BRONZE",
                LoyaltyPoints = 0,
                IsActive = true
            };
            await Repo.CreateAsync(customer);
            await Repo.SaveAsync();
            return customer.Id;
        }

        private async Task TrySendReservationConfirmationAsync(
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

        private async Task<List<Table>> GetAndValidateTables(List<Guid> tableIds)
        {
            var tables = new List<Table>();
            foreach (var tableId in tableIds)
            {
                var table = await Repo.GetOneAsync<Table>(
                    filter: t => t.Id == tableId && t.IsActive,
                    includeProperties: "Floor");
                if (table == null)
                    throw new KeyNotFoundException($"Table not found: {tableId}");
                tables.Add(table);
            }
            return tables;
        }

        private static void ValidateCapacity(int numberOfGuests, List<Table> tables)
        {
            var totalCapacity = tables.Sum(t => t.SeatingCapacity);
            if (numberOfGuests > totalCapacity)
                throw new InvalidOperationException(
                    $"Number of guests ({numberOfGuests}) exceeds total table capacity ({totalCapacity})");
        }

        private async Task ApplyTableStatusSideEffect(Reservation reservation, string newStatusCode)
        {
            TableStatus? newTableStatus = newStatusCode switch
            {
                CompletedCode or CancelledCode => TableStatus.Available,
                _ => null
            };

            if (newTableStatus.HasValue)
            {
                foreach (var rt in reservation.ReservationTables)
                {
                    rt.Table.TableStatusId = newTableStatus.Value;
                    Repo.Update(rt.Table);
                }
            }
            if (newStatusCode == CompletedCode)
            {
                var activeSessions = (await Repo.GetAsync<TableSession>(
                    filter: ts => ts.ReservationId == reservation.Id && ts.IsActive)).ToList();
                foreach (var session in activeSessions)
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                    Repo.Update(session);
                }
            }
        }

        private async Task<Reservation?> GetReservationWithInfo(Guid id)
        {
            return await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == id,
                includeProperties: "ReservationTables.Table.Floor,Customer.ApplicationUser,ReservationStatus");
        }
        private static string GenerateConfirmationCode(Guid id)
            => "RX-" + id.ToString("N")[..6].ToUpper();

        #endregion
    }
}

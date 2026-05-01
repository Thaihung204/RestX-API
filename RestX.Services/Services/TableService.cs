using AutoMapper;
using Microsoft.AspNetCore.Http;
using QRCoder;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Orders;
using RestX.Models.Reservations;
using RestX.Models.Tables;
using RestX.Models.Tenants;
using System.Text.RegularExpressions;

namespace RestX.BLL.Services
{
    public class TableService : BaseService, ITableService
    {
        private readonly IMapper mapper;
        private readonly ICloudinaryService cloudinaryService;
        private int ReservationBufferMinutes => CurrentTenant?.Configuration?.SessionBufferMinutes ?? 120;

        public TableService(
            IMapper mapper,
            ICloudinaryService cloudinaryService,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
            this.cloudinaryService = cloudinaryService;
        }

        public async Task<IEnumerable<TableItem>> GetAllTables()
        {
            List<Table> tables = (await Repo.GetAllAsync<Table>(
                        includeProperties: "Table3DModel,Floor"
                    )).ToList();

            tables = tables
                .OrderBy(t => t.Code, NaturalTableCodeComparer.Instance)
                .ToList();

            return mapper.Map<List<TableItem>>(tables);
        }

        public async Task<TableItem?> GetTableById(Guid id)
        {
            var table = await Repo.GetOneAsync<Table>(
                filter: t => t.Id == id,
                includeProperties: "Table3DModel,Floor"
            );
            return mapper.Map<TableItem>(table);
        }

        public async Task<TableItem> UpsertTable(Guid? id, TableItem request)
        {
            string folderName = $"{CurrentTenant.Name.Replace(" ", "")}/tableCube/";

            async Task<string?> UploadImageAsync(IFormFile? file, string face)
            {
                if (file == null) return null;
                using var stream = file.OpenReadStream();
                var result = await cloudinaryService.UploadAsync(stream, file.FileName, folderName, $"{face}_{id}");
                return result.Url;
            }

            Table table;

            if (id.HasValue && id.Value != Guid.Empty)
            {
                table = await Repo.GetByIdAsync<Table>(id.Value);
                if (table == null)
                    throw new InvalidOperationException("Table not found");

                if (request.SeatingCapacity < 1)
                    throw new AppException("Seating capacity must be at least 1");

                if (request.SeatingCapacity < table.SeatingCapacity)
                    await ThrowIfSeatingCapacityConflicts(id.Value, request.SeatingCapacity);

                if (!request.IsActive && table.IsActive)
                    await ThrowIfTableHasActiveConstraints(id.Value);

                table.FloorId = request.FloorId;
                table.Code = request.Code;
                table.Type = request.Type;
                table.Shape = request.Shape;
                table.SeatingCapacity = request.SeatingCapacity;
                table.PositionX = request.PositionX;
                table.PositionY = request.PositionY;
                table.Width = request.Width;
                table.Height = request.Height;
                table.Rotation = request.Rotation;
                table.Has3DView = request.Has3DView;
                table.ViewDescription = request.ViewDescription;
                table.DefaultViewUrl = request.DefaultViewUrl;
                table.TableStatusId = request.TableStatusId;
                table.IsActive = request.IsActive;

                if (request.ClearCubemap)
                {
                    var faces = new[] { "Front", "Back", "Left", "Right", "Top", "Bottom" };
                    var deleteTasks = faces.Select(face =>
                        cloudinaryService.DeleteAsync($"{folderName}{face}_{id}"));
                    await Task.WhenAll(deleteTasks);

                    table.CubeFrontImageUrl = null;
                    table.CubeBackImageUrl = null;
                    table.CubeLeftImageUrl = null;
                    table.CubeRightImageUrl = null;
                    table.CubeTopImageUrl = null;
                    table.CubeBottomImageUrl = null;
                }
                else
                {
                    var uploadTasks = new[]
                    {
                        UploadImageAsync(request.CubeFrontImage, "Front"),
                        UploadImageAsync(request.CubeBackImage, "Back"),
                        UploadImageAsync(request.CubeLeftImage, "Left"),
                        UploadImageAsync(request.CubeRightImage, "Right"),
                        UploadImageAsync(request.CubeTopImage, "Top"),
                        UploadImageAsync(request.CubeBottomImage, "Bottom")
                    };
                    var uploadedUrls = await Task.WhenAll(uploadTasks);

                    if (uploadedUrls[0] != null) table.CubeFrontImageUrl = uploadedUrls[0];
                    if (uploadedUrls[1] != null) table.CubeBackImageUrl = uploadedUrls[1];
                    if (uploadedUrls[2] != null) table.CubeLeftImageUrl = uploadedUrls[2];
                    if (uploadedUrls[3] != null) table.CubeRightImageUrl = uploadedUrls[3];
                    if (uploadedUrls[4] != null) table.CubeTopImageUrl = uploadedUrls[4];
                    if (uploadedUrls[5] != null) table.CubeBottomImageUrl = uploadedUrls[5];
                }

                Repo.Update(table);
            }
            else
            {
                table = mapper.Map<Table>(request);
                table.TableStatusId = TableStatus.Available;
                await Repo.CreateAsync(table);
            }
            await Repo.SaveAsync();

            if (string.IsNullOrEmpty(table.QRCodeUrl) && CurrentTenant != null)
            {
                table.QRCodeUrl = GenerateTableQRCode(table.Id, CurrentTenant.Hostname);
                Repo.Update(table);
                await Repo.SaveAsync();
            }
            await RedisService.RemoveAsync($"Floor:{CurrentTenant?.Hostname}");
            return mapper.Map<TableItem>(table);
        }

        public async Task DeleteTable(Guid id)
        {
            var table = await Repo.GetByIdAsync<Table>(id);
            if (table == null)
                return;
            await ThrowIfTableHasActiveConstraints(id);
            Repo.Delete<Table>(id);
            await Repo.SaveAsync();
            await RedisService.RemoveAsync($"Floor:{CurrentTenant?.Hostname}");
        }

        public async Task<TableItem> ChangeTableStatus(Guid tableId, TableStatus status)
        {
            var table = await Repo.GetByIdAsync<Table>(tableId);

            if (status == TableStatus.Available)
            {
                var hasActiveSession = await Repo.GetExistsAsync<TableSession>(
                    filter: ts => ts.TableId == tableId && ts.IsActive);
                if (hasActiveSession)
                    throw new AppException("Cannot set table to Available while it has an active session");
            }

            table.TableStatusId = status;

            Repo.Update(table);
            await Repo.SaveAsync();

            return mapper.Map<TableItem>(table);
        }

        public async Task<TableSession> CreateTableSession(Guid tableId, string userId, Guid? customerId = null, Guid? reservationId = null)
        {
            var isExistTable = await Repo.GetExistsAsync<Table>(filter: t => t.Id == tableId && t.IsActive);
            if (!isExistTable)
                throw new AppException("Table not found");

            DateTime now = DateTime.UtcNow.AddHours(7);
            DateTime startedAt = now;
            DateTime endedAt = now.AddMinutes(ReservationBufferMinutes);
            if (reservationId.HasValue)
            {
                var reservation = await Repo.GetByIdAsync<Reservation>(reservationId.Value);
                if (reservation != null)
                {
                    startedAt = reservation.Time;
                    endedAt = reservation.Time.AddMinutes(ReservationBufferMinutes);
                }
            }

            TableSession newSession = new TableSession
            {
                TableId = tableId,
                ReservationId = reservationId,
                IsActive = true,
                StartedAt = startedAt,
                EndedAt = endedAt
            };

            await Repo.CreateAsync(newSession, userId);

            return newSession;
        }

        public async Task<TableSession?> GetActiveTableSession(Guid tableId)
        {
            var now = DateTime.UtcNow.AddHours(7);

            var session = await Repo.GetOneAsync<TableSession>(
                filter: ts => ts.TableId == tableId && ts.IsActive
                           && ts.StartedAt <= now
                           && (ts.EndedAt == null || ts.EndedAt > now)
            );

            return session;
        }

        public async Task CloseTableSession(List<Guid> tableIds)
        {
            List<Guid> normalizedTableIds = (tableIds ?? new List<Guid>())
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            if (!normalizedTableIds.Any())
                throw new AppException("TableIds is required");

            DateTime now = DateTime.UtcNow.AddHours(7);

            List<TableSession> targetSessions = (await Repo.GetAsync<TableSession>(
                filter: ts => normalizedTableIds.Contains(ts.TableId) && ts.IsActive
            )).ToList();

            List<Guid> orderIds = targetSessions
                .Where(ts => ts.OrderId.HasValue)
                .Select(ts => ts.OrderId!.Value)
                .Distinct()
                .ToList();

            List<TableSession> allOrderSessions = new();

            if (orderIds.Any())
            {
                allOrderSessions = (await Repo.GetAsync<TableSession>(
                    filter: ts => ts.OrderId.HasValue && orderIds.Contains(ts.OrderId.Value) && ts.IsActive
                )).ToList();
            }

            foreach (Guid orderId in orderIds)
            {
                List<TableSession> orderSessions = allOrderSessions
                    .Where(ts => ts.OrderId == orderId)
                    .ToList();

                bool allIncluded = orderSessions.All(ts => normalizedTableIds.Contains(ts.TableId));

                if (allIncluded)
                {
                    Order? order = await Repo.GetByIdAsync<Order>(orderId);
                    if (order != null && order.OrderStatusId == (int)OrderStatus.Open)
                        throw new AppException("Cannot close table session while order is open");
                }

                foreach (TableSession session in targetSessions.Where(ts => ts.OrderId == orderId))
                {
                    session.IsActive = false;
                    session.EndedAt = now;
                    Repo.Update(session);
                }
            }

            foreach (TableSession session in targetSessions.Where(ts => ts.OrderId == null))
            {
                session.IsActive = false;
                session.EndedAt = now;
                Repo.Update(session);
            }

            await Repo.SaveAsync();
        }
        public async Task<IEnumerable<TableSessionInfo>> GetAllTableSession(DateTime? at = null)
        {
            List<TableSession> sessions;
            DateTime targetTime;

            if (at.HasValue)
            {
                targetTime = at.Value.AddMinutes(-1);

                sessions = (await Repo.GetAsync<TableSession>(
                    filter: ts => ts.StartedAt <= targetTime
                               && (ts.EndedAt == null || ts.EndedAt >= targetTime),
                    includeProperties: "Table,Order,Reservation,Reservation.Customer,Reservation.Customer.ApplicationUser"
                )).ToList();
            }
            else
            {
                targetTime = DateTime.UtcNow.AddHours(7);

                sessions = (await Repo.GetAsync<TableSession>(
                    filter: ts => ts.IsActive
                               && ts.StartedAt <= targetTime,
                    includeProperties: "Table,Order,Reservation,Reservation.Customer,Reservation.Customer.ApplicationUser"
                )).ToList();
            }

            sessions = sessions
                .OrderBy(ts => ts.Table?.Code, NaturalTableCodeComparer.Instance)
                .ToList();

            return mapper.Map<List<TableSessionInfo>>(sessions);
        }

        public async Task<MergeTableResponse> MergeTable(MergeTableRequest request, string userId)
        {
            List<Guid> tableIds = (request.TableIds ?? new List<Guid>())
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            if (!tableIds.Any())
                throw new AppException("TableIds is required");

            // Validate tables exist & active
            var tables = (await Repo.GetAsync<Table>(t => tableIds.Contains(t.Id) && t.IsActive)).ToList();
            if (tables.Count != tableIds.Count)
                throw new AppException("One or more tables not found or inactive");

            DateTime now = DateTime.UtcNow.AddHours(7);

            // Load active sessions for these tables (include Table and Order)
            List<TableSession> sessions = (await Repo.GetAsync<TableSession>(
                filter: ts => tableIds.Contains(ts.TableId)
                           && ts.IsActive
                           && ts.StartedAt <= now
                           && (ts.EndedAt == null || ts.EndedAt > now),
                includeProperties: "Table,Order")).ToList();

            foreach (Guid tableId in tableIds)
            {
                if (!sessions.Any(s => s.TableId == tableId))
                {
                    TableSession newSession = await CreateTableSession(tableId, userId, request.CustomerId, request.ReservationId);
                    newSession = await Repo.GetOneAsync<TableSession>(filter: ts => ts.Id == newSession.Id, includeProperties: "Table,Order");

                    if (request.ReservationId.HasValue && newSession.StartedAt > now)
                    {
                        newSession.StartedAt = now;
                        newSession.EndedAt = now.AddMinutes(ReservationBufferMinutes);
                        Repo.Update(newSession);
                    }

                    sessions.Add(newSession);
                }
            }

            List<Guid> orderIds = sessions
                .Where(s => s.OrderId.HasValue)
                .Select(s => s.OrderId!.Value)
                .Distinct()
                .ToList();

            var response = new MergeTableResponse
            {
                ExistingOrderIds = orderIds,
                Sessions = mapper.Map<List<RestX.BLL.DataTranferObjects.Table.TableSessionInfo>>(sessions)
            };

            if (!orderIds.Any())
            {
                response.OrderId = null;
                response.RequiresManualResolution = false;
                response.Message = "No existing order. Sessions created/validated.";
                await Repo.SaveAsync();
                return response;
            }

            // Case: exactly one order -> assign it to all sessions
            if (orderIds.Count == 1)
            {
                Guid targetOrderId = orderIds[0];
                foreach (var session in sessions.Where(s => s.OrderId != targetOrderId))
                {
                    session.OrderId = targetOrderId;
                    Repo.Update(session);
                }

                await Repo.SaveAsync();

                response.OrderId = targetOrderId;
                response.RequiresManualResolution = false;
                response.Message = "Merged successfully to existing order.";
                response.Sessions = mapper.Map<List<RestX.BLL.DataTranferObjects.Table.TableSessionInfo>>(sessions);
                return response;
            }

            // Case: multiple orders -> require manual resolution
            response.OrderId = null;
            response.RequiresManualResolution = true;
            response.Message = "Multiple existing orders found. Manual resolution required.";
            response.Sessions = mapper.Map<List<RestX.BLL.DataTranferObjects.Table.TableSessionInfo>>(sessions);
            return response;
        }

        //public async Task<SplitTableResponse> SplitTable(SplitTableRequest request)
        //{
        //    List<Guid> tableIds = (request.TableIds ?? new List<Guid>())
        //        .Where(x => x != Guid.Empty)
        //        .Distinct()
        //        .ToList();

        //    if (!tableIds.Any())
        //        throw new AppException("TableIds is required");

        //    var tables = (await Repo.GetAsync<Table>(t => tableIds.Contains(t.Id) && t.IsActive)).ToList();
        //    if (tables.Count != tableIds.Count)
        //        throw new AppException("One or more tables not found or inactive");

        //    DateTime now = DateTime.UtcNow.AddHours(7);

        //    List<TableSession> sessions = (await Repo.GetAsync<TableSession>(
        //        filter: ts => tableIds.Contains(ts.TableId)
        //                   && ts.IsActive
        //                   && ts.StartedAt <= now
        //                   && (ts.EndedAt == null || ts.EndedAt > now),
        //        includeProperties: "Table,Order")).ToList();

        //    if (sessions.Count != tableIds.Count)
        //        throw new AppException("One or more tables do not have an active session");

        //    List<Guid> orderIds = sessions
        //        .Where(s => s.OrderId.HasValue)
        //        .Select(s => s.OrderId!.Value)
        //        .Distinct()
        //        .ToList();

        //    var response = new SplitTableResponse
        //    {
        //        ExistingOrderIds = orderIds,
        //        Sessions = mapper.Map<List<RestX.BLL.DataTranferObjects.Table.TableSessionInfo>>(sessions)
        //    };

        //    if (!orderIds.Any())
        //    {
        //        response.OrderId = null;
        //        response.RequiresManualResolution = false;
        //        response.Message = "No existing order to split.";
        //        return response;
        //    }

        //    if (orderIds.Count > 1)
        //    {
        //        response.OrderId = null;
        //        response.RequiresManualResolution = true;
        //        response.Message = "Multiple existing orders found. Manual resolution required.";
        //        return response;
        //    }

        //    Guid targetOrderId = orderIds[0];

        //    foreach (TableSession session in sessions.Where(s => s.OrderId == targetOrderId))
        //    {
        //        session.OrderId = null;
        //        Repo.Update(session);
        //    }

        //    await Repo.SaveAsync();

        //    response.OrderId = targetOrderId;
        //    response.RequiresManualResolution = false;
        //    response.Message = "Split successfully. Order detached from selected tables.";
        //    response.Sessions = mapper.Map<List<RestX.BLL.DataTranferObjects.Table.TableSessionInfo>>(sessions);
        //    return response;
        //}

        private sealed class NaturalTableCodeComparer : IComparer<string?>
        {
            public static NaturalTableCodeComparer Instance { get; } = new NaturalTableCodeComparer();

            public int Compare(string? x, string? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                MatchCollection xParts = Regex.Matches(x, @"\d+|\D+");
                MatchCollection yParts = Regex.Matches(y, @"\d+|\D+");

                int max = Math.Min(xParts.Count, yParts.Count);

                for (int i = 0; i < max; i++)
                {
                    string xPart = xParts[i].Value;
                    string yPart = yParts[i].Value;

                    bool xIsNumber = int.TryParse(xPart, out int xNumber);
                    bool yIsNumber = int.TryParse(yPart, out int yNumber);

                    int result;
                    if (xIsNumber && yIsNumber)
                    {
                        result = xNumber.CompareTo(yNumber);
                        if (result != 0)
                        {
                            return result;
                        }

                        result = xPart.Length.CompareTo(yPart.Length);
                        if (result != 0)
                        {
                            return result;
                        }
                    }
                    else
                    {
                        result = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                        if (result != 0)
                        {
                            return result;
                        }
                    }
                }

                return xParts.Count.CompareTo(yParts.Count);
            }
        }

        #region Constraint Helpers

        private async Task ThrowIfTableHasActiveConstraints(Guid tableId)
        {
            var hasActiveSession = await Repo.GetExistsAsync<TableSession>(
                filter: ts => ts.TableId == tableId && ts.IsActive);
            if (hasActiveSession)
                throw new AppException("Cannot perform this action while the table has an active session");

            var now = DateTime.UtcNow.AddHours(7);
            var hasFutureReservation = await Repo.GetExistsAsync<TableSession>(
                filter: ts => ts.TableId == tableId
                           && ts.ReservationId != null
                           && ts.Reservation.Time > now
                           && (ts.Reservation.ReservationStatus.Code == "PENDING"
                               || ts.Reservation.ReservationStatus.Code == "CONFIRMED"));
            if (hasFutureReservation)
                throw new AppException("Cannot perform this action while the table has upcoming reservations");
        }

        private async Task ThrowIfSeatingCapacityConflicts(Guid tableId, int newCapacity)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var hasConflict = await Repo.GetExistsAsync<TableSession>(
                filter: ts => ts.TableId == tableId
                           && ts.ReservationId != null
                           && ts.Reservation.Time > now
                           && ts.Reservation.ReservationStatus.Code == "CONFIRMED"
                           && ts.Reservation.NumberOfGuests > newCapacity);
            if (hasConflict)
                throw new AppException("Cannot reduce seating capacity below the guest count of a future confirmed reservation");
        }

        #endregion

        #region QR Code Generation
        private string GenerateTableQRCode(Guid tableId, string tenantHostname)
        {
            var url = $"https://{tenantHostname}/customer/{tableId}";
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeBytes = qrCode.GetGraphic(20);
                    string base64String = Convert.ToBase64String(qrCodeBytes);
                    return $"data:image/png;base64,{base64String}";
                }
            }
        }
        #endregion
    }
}

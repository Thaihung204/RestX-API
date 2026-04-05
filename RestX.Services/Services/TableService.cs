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

namespace RestX.BLL.Services
{
    public class TableService : BaseService, ITableService
    {
        private readonly IMapper mapper;
        private readonly ICloudinaryService cloudinaryService;
        private const int ReservationBufferMinutes = 120;

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
            var tables = (await Repo.GetAllAsync<Table>(
                        orderBy: q => q.OrderBy(t => t.Code),
                        includeProperties: "Table3DModel,Floor"
                    )).ToList();
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
            Repo.Delete<Table>(id);
            await Repo.SaveAsync();
            await RedisService.RemoveAsync($"Floor:{CurrentTenant?.Hostname}");
        }

        public async Task<TableItem> ChangeTableStatus(Guid tableId, TableStatus status)
        {
            var table = await Repo.GetByIdAsync<Table>(tableId);

            table.TableStatusId = status;

            Repo.Update(table);
            await Repo.SaveAsync();

            return mapper.Map<TableItem>(table);
        }

        public async Task<TableSession> CreateTableSession(Guid tableId, string userId, Guid? customerId = null, Guid? reservationId = null)
        {
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

            var session = await Repo.GetAsync<TableSession>(
                filter: ts => ts.TableId == tableId && ts.IsActive
                           && ts.StartedAt <= now
                           && (ts.EndedAt == null || ts.EndedAt > now), 
                           orderBy: q => q.OrderByDescending(ts => ts.CreatedDate)
            );

            return session.FirstOrDefault();
        }

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

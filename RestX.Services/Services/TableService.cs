using AutoMapper;
using QRCoder;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Enum;
using RestX.Models.Tables;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class TableService : BaseService, ITableService
    {
        private readonly IMapper mapper;
        public TableService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
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
            return mapper.Map<TableItem>(table);
        }

        public async Task DeleteTable(Guid id)
        {
            var table = await Repo.GetByIdAsync<Table>(id);
            if (table == null)
                return;
            Repo.Delete<Table>(id);
            await Repo.SaveAsync();
        }

        public async Task<TableItem> ChangeTableStatus(Guid tableId, TableStatus status)
        {
            var table = await Repo.GetByIdAsync<Table>(tableId);

            table.TableStatusId = status;

            Repo.Update(table);
            await Repo.SaveAsync();

            return mapper.Map<TableItem>(table);
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

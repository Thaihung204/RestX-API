using AutoMapper;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
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

        private string GetCacheKey()
            => $"{CurrentTenant?.Id}:tables";
        public async Task<IEnumerable<TableItem>> GetAllTables()
        {
            var tables = await RedisService.GetAsync<List<Table>>(GetCacheKey());
            if (tables == null)
            {
                tables = (await Repo.GetAllAsync<Table>(
                        orderBy: q => q.OrderBy(t => t.Code),
                        includeProperties: "Table3DModel"
                    )).ToList();
                await RedisService.SetAsync(GetCacheKey(), tables);
            }
            return mapper.Map<List<TableItem>>(tables);
        }

        public async Task<TableItem?> GetTableById(Guid id)
        {
            var table = await Repo.GetOneAsync<Table>(
                filter: t => t.Id == id,
                includeProperties: "Table3DModel"
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
                await Repo.CreateAsync(table);
            }
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey());
            return mapper.Map<TableItem>(table);
        }

        public async Task DeleteTable(Guid id)
        {
            var table = await Repo.GetByIdAsync<Table>(id);
            if (table == null)
                return;
            Repo.Delete<Table>(id);
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey());
        }
    }
}

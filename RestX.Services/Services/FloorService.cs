using AutoMapper;
using RestX.BLL.DataTranferObjects.Floor;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Tables;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class FloorService : BaseService, IFloorService
    {
        private readonly ICloudinaryService cloudinaryService;
        private readonly IMapper mapper;

        public FloorService(
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            this.cloudinaryService = cloudinaryService;
            this.mapper = mapper;
        }

        private string GetCacheKey() => $"Floor:{CurrentTenant.Hostname}";

        public async Task<IEnumerable<FloorItem>> GetAllFloors()
        {
            var cached = await RedisService.GetAsync<List<FloorItem>>(GetCacheKey());
            if (cached != null) return cached;
            var floors = (await Repo.GetAllAsync<Floor>(
                orderBy: q => q.OrderBy(f => f.Name),
                includeProperties: "Tables"
            )).ToList();
            var result = mapper.Map<List<FloorItem>>(floors);
            await RedisService.SetAsync(GetCacheKey(), result);
            return result;
        }

        public async Task<FloorItem?> GetFloorById(Guid id)
        {
            var floor = await Repo.GetOneAsync<Floor>(
                filter: f => f.Id == id,
                includeProperties: "Tables"
            );
            if (floor == null) return null;
            return mapper.Map<FloorItem>(floor);
        }

        public async Task<FloorItem> UpsertFloor(FloorItem request, string? currentUser = null)
        {
            Floor floor;
            if (request.Id != null)
            {
                floor = await Repo.GetOneAsync<Floor>(
                    filter: f => f.Id == request.Id,
                    includeProperties: "Tables"
                );

                if (floor == null)
                    throw new InvalidOperationException("Floor not found");
                floor.Name = request.Name;
                floor.Width = request.Width;
                floor.Height = request.Height;
                floor.IsActive = request.IsActive;
                if (request.Image != null)
                {
                    if (!string.IsNullOrEmpty(floor.ImageUrl))
                    {
                        await cloudinaryService.DeleteAsync($"{CurrentTenant.Name.Replace(" ", "")}/floors/{floor.Id}");
                    }
                    using var stream = request.Image.OpenReadStream();
                    var uploadResult = await cloudinaryService.UploadAsync(
                        fileStream: stream,
                        fileName: request.Image.FileName,
                        folder: $"{CurrentTenant.Name.Replace(" ", "")}/floors",
                        publicId: floor.Id.ToString(),
                        overwrite: true
                    );
                    floor.ImageUrl = uploadResult.Url;
                }
                Repo.Update(floor, currentUser);
            }
            else
            {
                floor = mapper.Map<Floor>(request);
                floor.Id = Guid.NewGuid();
                if (request.Image != null)
                {
                    using var stream = request.Image.OpenReadStream();
                    var uploadResult = await cloudinaryService.UploadAsync(
                        fileStream: stream,
                        fileName: request.Image.FileName,
                        folder: $"{CurrentTenant.Name.Replace(" ", "")}/floors",
                        publicId: floor.Id.ToString(),
                        overwrite: true
                    );
                    floor.ImageUrl = uploadResult.Url;
                }
                Repo.Create(floor, currentUser);
            }
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey());
            return mapper.Map<FloorItem>(floor);
        }

        public async Task<FloorLayoutResponse?> GetFloorLayout(Guid floorId)
        {
            var floor = await Repo.GetOneAsync<Floor>(
                filter: f => f.Id == floorId,
                includeProperties: "Tables"
            );
            if (floor == null) return null;
            return new FloorLayoutResponse
            {
                Floor = new FloorLayoutInfo
                {
                    Id = floor.Id,
                    Name = floor.Name,
                    Width = floor.Width,
                    Height = floor.Height,
                    BackgroundImageUrl = floor.ImageUrl
                },
                Tables = floor.Tables.Select(t => new TableLayoutItem
                {
                    Id = t.Id,
                    Code = t.Code,
                    SeatingCapacity = t.SeatingCapacity,
                    Status = t.TableStatusId.ToString(),
                    Layout = new TableLayoutPosition
                    {
                        X = t.PositionX,
                        Y = t.PositionY,
                        Width = t.Width,
                        Height = t.Height,
                        Rotation = t.Rotation,
                        Shape = t.Shape
                    }
                }).ToList()
            };
        }

        public async Task<bool> SaveLayout(Guid floorId, SaveLayoutRequest request, string? currentUser = null)
        {
            var floor = await Repo.GetOneAsync<Floor>(
                filter: f => f.Id == floorId,
                includeProperties: "Tables"
            );
            if (floor == null) return false;
            var tableDict = floor.Tables.ToDictionary(t => t.Id);
            foreach (var item in request.Tables)
            {
                if (!tableDict.TryGetValue(item.Id, out var table)) continue;
                table.PositionX = item.X;
                table.PositionY = item.Y;
                table.Width = item.Width;
                table.Height = item.Height;
                table.Rotation = item.Rotation;
                Repo.Update(table, currentUser);
            }
            await Repo.SaveAsync();
            return true;
        }

        public async Task<bool> DeleteFloor(Guid id)
        {
            var floor = await Repo.GetByIdAsync<Floor>(id);
            if (floor == null) return false;
            if (!string.IsNullOrEmpty(floor.ImageUrl))
            {
                await cloudinaryService.DeleteAsync($"{CurrentTenant.Name.Replace(" ", "")}/floors/{floor.Id}");
            }
            Repo.Delete(floor);
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey());
            return true;
        }
    }
}

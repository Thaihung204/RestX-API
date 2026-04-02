using AutoMapper;
using RestX.BLL.DataTranferObjects.Floor;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Enum;
using RestX.Models.Reservations;
using RestX.Models.Tenants;
using FloorEntity = RestX.Models.Tables.Floor;

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

        public async Task<IEnumerable<Floor>> GetAllFloors()
        {
            var floors = (await Repo.GetAllAsync<FloorEntity>(
                orderBy: q => q.OrderBy(f => f.Name),
                includeProperties: "Tables"
            )).ToList();
            return mapper.Map<List<Floor>>(floors);
        }

        public async Task<Floor?> GetFloorById(Guid id)
        {
            var floor = await Repo.GetOneAsync<FloorEntity>(
                filter: f => f.Id == id,
                includeProperties: "Tables"
            );
            if (floor == null) return null;
            return mapper.Map<Floor>(floor);
        }

        public async Task<Guid> UpsertFloor(Floor request, string? currentUser = null)
        {
            FloorEntity floor;
            if (request.Id != null)
            {
                floor = await Repo.GetOneAsync<FloorEntity>(
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
                floor = mapper.Map<FloorEntity>(request);
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
            return floor.Id;
        }

        private const int ReservationBufferMinutes = 120;

        public async Task<FloorLayoutResponse?> GetFloorLayout(Guid floorId, DateTime? at = null)
        {
            var floor = await Repo.GetOneAsync<FloorEntity>(
                filter: f => f.Id == floorId,
                includeProperties: "Tables"
            );
            if (floor == null) return null;

            HashSet<Guid> occupiedTableIds = new();

            if (at.HasValue)
            {
                var bufferStart = at.Value.AddMinutes(-ReservationBufferMinutes);
                var bufferEnd = at.Value.AddMinutes(ReservationBufferMinutes);

                occupiedTableIds = (await Repo.GetAsync<TableSession>(
                    filter: ts => ts.IsActive && ts.StartedAt.AddMinutes(ReservationBufferMinutes) > at.Value
                )).Select(ts => ts.TableId).ToHashSet();
            }

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
                Tables = floor.Tables.Select(t =>
                {
                    //var status = at.HasValue
                    //    ? (occupiedTableIds.Contains(t.Id) ? TableStatus.Occupied
                    //        : reservedTableIds.Contains(t.Id) ? TableStatus.Reserved
                    //        : TableStatus.Available)
                    //    : t.TableStatusId;

                    return new TableLayoutItem
                    {
                        Id = t.Id,
                        Code = t.Code,
                        SeatingCapacity = t.SeatingCapacity,
                        //Status = status.ToString(),
                        Layout = new TableLayoutPosition
                        {
                            X = t.PositionX,
                            Y = t.PositionY,
                            Width = t.Width,
                            Height = t.Height,
                            Rotation = t.Rotation,
                            Shape = t.Shape
                        },
                        CubeFrontImageUrl = t.CubeFrontImageUrl,
                        CubeBackImageUrl = t.CubeBackImageUrl,
                        CubeLeftImageUrl = t.CubeLeftImageUrl,
                        CubeRightImageUrl = t.CubeRightImageUrl,
                        CubeTopImageUrl = t.CubeTopImageUrl,
                        CubeBottomImageUrl = t.CubeBottomImageUrl
                    };
                }).ToList()
            };
        }

        public async Task<bool> SaveLayout(Guid floorId, SaveLayoutRequest request, string? currentUser = null)
        {
            var floor = await Repo.GetOneAsync<FloorEntity>(
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
            var floor = await Repo.GetByIdAsync<FloorEntity>(id);
            if (floor == null) return false;
            if (!string.IsNullOrEmpty(floor.ImageUrl))
            {
                await cloudinaryService.DeleteAsync($"{CurrentTenant.Name.Replace(" ", "")}/floors/{floor.Id}");
            }
            Repo.Delete(floor);
            await Repo.SaveAsync();
            return true;
        }
    }
}

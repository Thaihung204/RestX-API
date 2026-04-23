using AutoMapper;
using RestX.BLL.DataTranferObjects.Status;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Status;
using RestX.Models.Common;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class StatusValueService : BaseService, IStatusValueService
    {
        private readonly IMapper mapper;
        public StatusValueService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }

        private string GetCacheKey(string typeCode)
            => $"StatusValue:{CurrentTenant?.Hostname}:{typeCode.ToUpperInvariant()}";

        public async Task<IEnumerable<StatusValues>> GetStatuses(string typeCode)
        {
            var cacheKey = GetCacheKey(typeCode);
            var cached = await RedisService.GetAsync<List<StatusValues>>(cacheKey);
            if (cached != null)
                return cached;
            var statusType = await GetStatusType(typeCode);
            var values = (await Repo.GetAsync<StatusValue>(
                filter: sv => sv.StatusTypeId == statusType.Id,
                orderBy: q => q.OrderBy(sv => sv.DisplayOrder)
            )).ToList();
            var result = mapper.Map<List<StatusValues>>(values);
            await RedisService.SetAsync(cacheKey, result);
            return result;
        }

        public async Task<StatusValues?> GetStatusValueById(int id)
        {
            var value = await Repo.GetByIdAsync<StatusValue>(id);
            return value == null ? null : mapper.Map<StatusValues>(value);
        }

        public async Task<StatusValues> UpsertStatusValue(string typeCode, int? id, StatusValues request)
        {
            var statusType = await GetStatusType(typeCode);
            StatusValue entity;
            if (id.HasValue && id.Value > 0)
            {
                entity = await Repo.GetByIdAsync<StatusValue>(id.Value);
                if (entity == null)
                    throw new InvalidOperationException("Status value not found");
                entity.Code = request.Code;
                entity.Name = request.Name;
                entity.ColorCode = request.ColorCode;
                entity.IsDefault = request.IsDefault;
                entity.DisplayOrder = request.DisplayOrder > 0 ? request.DisplayOrder : 1;
                Repo.Update(entity);
            }
            else
            {
                entity = mapper.Map<StatusValue>(request);
                entity.StatusTypeId = statusType.Id;
                entity.IsSystem = false;
                var maxOrder = (await Repo.GetAsync<StatusValue>(
                    filter: sv => sv.StatusTypeId == statusType.Id,
                    orderBy: q => q.OrderByDescending(sv => sv.DisplayOrder),
                    take: 1
                )).FirstOrDefault()?.DisplayOrder ?? 0;
                entity.DisplayOrder = maxOrder + 1;
                await Repo.CreateAsync(entity);
            }
            if (request.IsDefault)
            {
                var others = (await Repo.GetAsync<StatusValue>(
                    filter: sv => sv.StatusTypeId == statusType.Id && sv.Id != entity.Id && sv.IsDefault
                )).ToList();
                foreach (var other in others)
                {
                    other.IsDefault = false;
                    Repo.Update(other);
                }
            }
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey(typeCode));
            return mapper.Map<StatusValues>(entity);
        }

        public async Task DeleteStatusValue(string typeCode, int id)
        {
            var statusType = await GetStatusType(typeCode);
            var entity = await Repo.GetByIdAsync<StatusValue>(id);
            if (entity == null || entity.StatusTypeId != statusType.Id)
                throw new InvalidOperationException("Status value not found");
            if (entity.IsSystem)
                throw new InvalidOperationException("Cannot delete a system status value");
            Repo.Delete<StatusValue>(id);
            if (entity.IsDefault)
            {
                var fallback = (await Repo.GetAsync<StatusValue>(
                    filter: sv => sv.StatusTypeId == statusType.Id && sv.Id != id,
                    orderBy: q => q.OrderBy(sv => sv.DisplayOrder),
                    take: 1
                )).FirstOrDefault();
                if (fallback != null)
                {
                    fallback.IsDefault = true;
                    Repo.Update(fallback);
                }
            }
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey(typeCode));
        }

        public async Task UpdateDisplayOrder(string typeCode, List<StatusValues> items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Danh sách không hợp lệ.");

            if (items.Any(i => i.Id <= 0 || i.DisplayOrder <= 0))
                throw new InvalidOperationException("Mỗi item phải có Id và DisplayOrder hợp lệ.");

            if (items.Select(i => i.Id).Distinct().Count() != items.Count)
                throw new InvalidOperationException("Danh sách bị trùng Id.");

            if (items.Select(i => i.DisplayOrder).Distinct().Count() != items.Count)
                throw new InvalidOperationException("Danh sách bị trùng DisplayOrder.");

            var statusType = await GetStatusType(typeCode);
            var ids = items.Select(i => i.Id).ToList();
            var entities = (await Repo.GetAsync<StatusValue>(
                filter: sv => ids.Contains(sv.Id) && sv.StatusTypeId == statusType.Id
            )).ToList();

            if (entities.Count != ids.Count)
                throw new InvalidOperationException("Một hoặc nhiều status value không tồn tại hoặc không thuộc type này.");

            foreach (var item in items)
            {
                var entity = entities.First(e => e.Id == item.Id);
                entity.DisplayOrder = item.DisplayOrder;
                Repo.Update(entity);
            }

            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey(typeCode));
        }

        private async Task<StatusType> GetStatusType(string typeCode)
        {
            var code = typeCode.ToUpperInvariant();
            var statusType = await Repo.GetOneAsync<StatusType>(
                filter: st => st.Code == code);
            return statusType
                ?? throw new InvalidOperationException($"Status type '{typeCode}' not found");
        }
    }
}
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
                orderBy: q => q.OrderBy(sv => sv.Id)
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
                Repo.Update(entity);
            }
            else
            {
                entity = mapper.Map<StatusValue>(request);
                entity.StatusTypeId = statusType.Id;
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
            Repo.Delete<StatusValue>(id);
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
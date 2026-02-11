using AutoMapper;
using RestX.BLL.DataTranferObjects.StatusValue;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.StatusValues;
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
            => $"{CurrentTenant?.Id}:status:{typeCode.ToUpperInvariant()}";

        private async Task<StatusType> GetStatusTypeOrThrow(string typeCode)
        {
            var statusType = await Repo.GetOneAsync<StatusType>(
                filter: st => st.Code == typeCode.ToUpperInvariant());
            return statusType
                ?? throw new InvalidOperationException($"Status type '{typeCode}' not found");
        }

        public async Task<IEnumerable<StatusValueItem>> GetByType(string typeCode)
        {
            var cacheKey = GetCacheKey(typeCode);
            var cached = await RedisService.GetAsync<List<StatusValueItem>>(cacheKey);
            if (cached != null)
                return cached;

            var statusType = await GetStatusTypeOrThrow(typeCode);
            var values = (await Repo.GetAsync<StatusValue>(
                filter: sv => sv.StatusTypeId == statusType.Id,
                orderBy: q => q.OrderBy(sv => sv.Id)
            )).ToList();

            var result = mapper.Map<List<StatusValueItem>>(values);
            await RedisService.SetAsync(cacheKey, result);
            return result;
        }

        public async Task<StatusValueItem?> GetById(int id)
        {
            var value = await Repo.GetByIdAsync<StatusValue>(id);
            return value == null ? null : mapper.Map<StatusValueItem>(value);
        }

        public async Task<StatusValueItem> Upsert(string typeCode, int? id, StatusValueItem request)
        {
            var statusType = await GetStatusTypeOrThrow(typeCode);
            StatusValue entity;

            if (id.HasValue && id.Value > 0)
            {
                entity = await Repo.GetByIdAsync<StatusValue>(id.Value);
                if (entity == null)
                    throw new InvalidOperationException("Status value not found");

                mapper.Map(request, entity);
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
            return mapper.Map<StatusValueItem>(entity);
        }

        public async Task Delete(string typeCode, int id)
        {
            var statusType = await GetStatusTypeOrThrow(typeCode);
            var entity = await Repo.GetByIdAsync<StatusValue>(id);
            if (entity == null || entity.StatusTypeId != statusType.Id)
               throw new InvalidOperationException("Status value not found");
            Repo.Delete<StatusValue>(id);
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey(typeCode));
        }
    }
}

using AutoMapper;
using RestX.BLL.DataTranferObjects.Loyalty;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Loyalty;
using RestX.Models.Loyalty;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class LoyaltyPointBandService : BaseService, ILoyaltyPointBandService
    {
        private readonly IMapper mapper;
        public LoyaltyPointBandService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }
        public async Task<IEnumerable<LoyaltyPointBandItem>> GetAllLoyaltyPointBands()
        {
            var bands = (await Repo.GetAllAsync<LoyaltyPointBand>(
                orderBy: q => q.OrderBy(b => b.Min)
            )).ToList();
            return mapper.Map<List<LoyaltyPointBandItem>>(bands);
        }
        public async Task<LoyaltyPointBandItem?> GetLoyaltyPointBandById(Guid id)
        {
            var band = await Repo.GetOneAsync<LoyaltyPointBand>(
                filter: b => b.Id == id
            );
            return mapper.Map<LoyaltyPointBandItem>(band);
        }
        public async Task<Guid> UpsertLoyaltyPointBand(LoyaltyPointBandItem item, string userId)
        {
            await ValidateLoyaltyBand(item);
            if (item.Id != null)
            {
                var band = await Repo.GetByIdAsync<LoyaltyPointBand>(item.Id.Value);
                if (band == null)
                    return Guid.Empty;
                band.Name = item.Name;
                band.Min = item.Min;
                band.Max = item.Max;
                band.DiscountPercentage = item.DiscountPercentage;
                band.BenefitDescription = item.BenefitDescription;
                band.LogoColor = item.LogoColor;
                band.IsActive = item.IsActive;

                Repo.Update(band, userId);
                await Repo.SaveAsync();
                return band.Id;
            }
            var newBand = new LoyaltyPointBand
            {
                Name = item.Name,
                Min = item.Min,
                Max = item.Max,
                DiscountPercentage = item.DiscountPercentage,
                BenefitDescription = item.BenefitDescription,
                LogoColor = item.LogoColor,
                IsActive = item.IsActive
            };
            await Repo.CreateAsync(newBand, userId);
            return newBand.Id;
        }

        public async Task<bool> DeleteLoyaltyPointBand(Guid id)
        {
            var band = await Repo.GetByIdAsync<LoyaltyPointBand>(id);
            if (band == null)
                return false;
            Repo.Delete<LoyaltyPointBand>(id);
            await Repo.SaveAsync();
            return true;
        }

        private async Task ValidateLoyaltyBand(LoyaltyPointBandItem item)
        {
            var currentId = item.Id ?? Guid.Empty;
            if (item.Max.HasValue && item.Max.Value <= item.Min)
                throw new ArgumentException("Max points must be greater than Min points");
            var nameExists = await Repo.GetExistsAsync<LoyaltyPointBand>(
                filter: b => b.Name == item.Name && b.Id != currentId
            );
            if (nameExists)
                throw new ArgumentException($"A loyalty point band named '{item.Name}' already exists");
            var maxValue = item.Max;
            var minValue = item.Min;
            var overlapping = await Repo.GetOneAsync<LoyaltyPointBand>(
                filter: b => b.Id != currentId &&
                             (maxValue == null || maxValue > b.Min) &&
                             (b.Max == null || b.Max > minValue)
            );
            if (overlapping != null)
            {
                var overlappingMax = overlapping.Max?.ToString() ?? "∞";
                var itemMax = item.Max?.ToString() ?? "∞";
                throw new ArgumentException(
                    $"Point range [{item.Min}, {itemMax}] overlaps with band '{overlapping.Name}' [{overlapping.Min}, {overlappingMax}]"
                );
            }
        }
    }
}

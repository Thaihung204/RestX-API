using AutoMapper;
using Newtonsoft.Json.Linq;
using RestX.BLL.DataTranferObjects.Promotion;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class PromotionService : BaseService, IPromotionService
    {
        private readonly IMapper mapper;

        public PromotionService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }

        public async Task<List<DataTranferObjects.Promotion.Promotion>> GetAllPromotions()
        {
            List<Models.Promotions.Promotion> promotions = (await Repo.GetAllAsync<Models.Promotions.Promotion>(
                orderBy: q => q.OrderByDescending(x => x.CreatedDate),
                includeProperties: "PromotionApplicableItems"
            )).ToList();

            List<DataTranferObjects.Promotion.Promotion> result = promotions.Select(MapToPromotionItem).ToList();

            return result;
        }

        public async Task<List<DataTranferObjects.Promotion.Promotion>> GetActivePromotions()
        {
            string cacheKey = $"{CurrentTenant.Hostname}:Promotions:Active";

            List<DataTranferObjects.Promotion.Promotion>? cached = await RedisService.GetAsync<List<DataTranferObjects.Promotion.Promotion>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            DateTime now = DateTime.UtcNow.AddHours(7);

            List<Models.Promotions.Promotion> promotions = (await Repo.GetAsync<Models.Promotions.Promotion>(
                filter: x => x.IsActive && x.ValidFrom <= now && x.ValidTo >= now,
                includeProperties: "PromotionApplicableItems"
            )).OrderByDescending(x => x.CreatedDate).ToList();

            List<DataTranferObjects.Promotion.Promotion> result = promotions.Select(MapToPromotionItem).ToList();

            await RedisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<DataTranferObjects.Promotion.Promotion> GetPromotionById(Guid id)
        {
            Models.Promotions.Promotion promotion = await Repo.GetOneAsync<Models.Promotions.Promotion>(
                filter: x => x.Id == id,
                includeProperties: "PromotionApplicableItems"
            );

            if (promotion == null)
            {
                throw new AppException("Promotion not found");
            }

            return MapToPromotionItem(promotion);
        }

        public async Task<DataTranferObjects.Promotion.Promotion?> GetPromotionByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            string normalizedCode = code.Trim().ToUpperInvariant();
            DateTime now = DateTime.UtcNow.AddHours(7);

            Models.Promotions.Promotion promotion = await Repo.GetOneAsync<Models.Promotions.Promotion>(
                filter: x =>
                    x.Code.ToUpper() == normalizedCode
                    && x.IsActive
                    && x.ValidFrom <= now
                    && x.ValidTo >= now,
                includeProperties: "PromotionApplicableItems"
            );

            if (promotion == null)
            {
                return null;
            }

            return MapToPromotionItem(promotion);
        }

        public async Task<Guid> UpsertPromotion(DataTranferObjects.Promotion.Promotion item)
        {
            Models.Promotions.Promotion promotion;

            if (!item.Id.HasValue || item.Id == Guid.Empty)
            {
                bool codeExists = await Repo.GetExistsAsync<Models.Promotions.Promotion>(x => x.Code == item.Code.Trim().ToUpperInvariant());
                if (codeExists)
                {
                    throw new AppException("Promotion code already exists");
                }

                promotion = mapper.Map<Models.Promotions.Promotion>(item);
                promotion.Code = item.Code.Trim().ToUpperInvariant();

                promotion.PromotionApplicableItems = item.ApplicableItems
                    .Select(x => new Models.Promotions.PromotionApplicableItem
                    {
                        DishId = x.DishId ?? null,
                        CategoryId = x.CategoryId ?? null,
                        ComboId = x.ComboId ?? null
                    })
                    .ToList();

                await Repo.CreateAsync(promotion);
            }
            else
            {
                promotion = await Repo.GetOneAsync<Models.Promotions.Promotion>(
                    filter: x => x.Id == item.Id.Value,
                    includeProperties: "PromotionApplicableItems"
                );

                if (promotion == null)
                {
                    throw new AppException("Promotion not found");
                }

                bool codeExists = await Repo.GetExistsAsync<Models.Promotions.Promotion>(
                    x => x.Code == item.Code.Trim().ToUpperInvariant() && x.Id != promotion.Id
                );
                if (codeExists)
                {
                    throw new AppException("Promotion code already exists");
                }

                promotion.Code = item.Code.Trim().ToUpperInvariant();
                promotion.Name = item.Name.Trim();
                promotion.DiscountValue = item.DiscountValue;
                promotion.DiscountType = item.DiscountType.Trim().ToUpperInvariant();
                promotion.MaxDiscountAmount = item.MaxDiscountAmount;
                promotion.MinOrderAmount = item.MinOrderAmount;
                promotion.UsageLimit = item.UsageLimit;
                promotion.UsagePerCustomer = item.UsagePerCustomer;
                promotion.ValidFrom = item.ValidFrom;
                promotion.ValidTo = item.ValidTo;
                promotion.IsActive = item.IsActive;

                if (promotion.PromotionApplicableItems != null && promotion.PromotionApplicableItems.Any())
                {
                    foreach (Models.Promotions.PromotionApplicableItem oldItem in promotion.PromotionApplicableItems.ToList())
                    {
                        Repo.Delete(oldItem);
                    }
                }

                promotion.PromotionApplicableItems = item.ApplicableItems
                    .Select(x => new Models.Promotions.PromotionApplicableItem
                    {
                        PromotionId = promotion.Id,
                        DishId = x.DishId,
                        CategoryId = x.CategoryId,
                        ComboId = x.ComboId
                    })
                    .ToList();

                Repo.Update(promotion);
            }

            await Repo.SaveAsync();
            await RedisService.RemoveAsync($"{CurrentTenant.Hostname}:Promotions:Active");
            return promotion.Id;
        }

        public async Task<bool> DeletePromotion(Guid id)
        {
            Models.Promotions.Promotion promotion = await Repo.GetByIdAsync<Models.Promotions.Promotion>(id);
            if (promotion == null)
            {
                return false;
            }

            promotion.IsActive = false;
            Repo.Update(promotion);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync($"{CurrentTenant.Hostname}:Promotions:Active");
            return true;
        }

        private DataTranferObjects.Promotion.Promotion MapToPromotionItem(Models.Promotions.Promotion promotion)
        {
            DataTranferObjects.Promotion.Promotion item = mapper.Map<DataTranferObjects.Promotion.Promotion>(promotion);

            item.ApplicableItems = promotion.PromotionApplicableItems?
                .Select(x => new DataTranferObjects.Promotion.PromotionApplicableItem
                {
                    Id = x.Id,
                    DishId = x.DishId,
                    CategoryId = x.CategoryId,
                    ComboId = x.ComboId,
                    DishName = x.Dish?.Name,
                    CategoryName = x.Category?.Name,
                    ComboName = x.Combo?.Name
                })
                .ToList() ?? new List<DataTranferObjects.Promotion.PromotionApplicableItem>();

            return item;
        }
    }
}
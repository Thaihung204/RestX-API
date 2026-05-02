using AutoMapper;
using Newtonsoft.Json.Linq;
using RestX.BLL.DataTranferObjects.Promotion;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Customers;
using RestX.Models.Loyalty;
using RestX.Models.Orders;
using RestX.Models.Promotions;
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

        public async Task<List<DataTranferObjects.Promotion.Promotion>> GetActivePromotions(Guid? userId = null)
        {
            DateTime now = DateTime.UtcNow.AddHours(7);

            List<Models.Promotions.Promotion> promotions = (await Repo.GetAsync<Models.Promotions.Promotion>(
                filter: x => x.IsActive && x.ValidFrom <= now && x.ValidTo >= now,
                includeProperties: "PromotionApplicableItems"
            )).OrderByDescending(x => x.CreatedDate).ToList();

            if (userId.HasValue)
            {
                Customer? customer = await Repo.GetOneAsync<Customer>(c => c.ApplicationUserId == userId.Value);

                if (customer != null)
                {
                    List<Guid> promotionIds = promotions.Select(p => p.Id).ToList();
                    List<Guid> orderIds = (await Repo.GetAsync<Order>(o => o.CustomerId == customer.Id))
                        .Select(o => o.Id)
                        .ToList();

                    if (orderIds.Any() && promotionIds.Any())
                    {
                        List<PromotionHistory> histories = (await Repo.GetAsync<PromotionHistory>(
                            ph => promotionIds.Contains(ph.PromotionId) && orderIds.Contains(ph.OrderId)
                        )).ToList();

                        Dictionary<Guid, int> usageByPromotionId = histories
                            .GroupBy(h => h.PromotionId)
                            .ToDictionary(g => g.Key, g => g.Count());

                        promotions = promotions
                            .Where(p =>
                                p.UsagePerCustomer <= 0
                                || !usageByPromotionId.TryGetValue(p.Id, out int used)
                                || used < p.UsagePerCustomer)
                            .ToList();
                    }

                    LoyaltyPointBand? band = await Repo.GetOneAsync<LoyaltyPointBand>(
                        b => b.IsActive && b.Name == customer.MembershipLevel);

                    List<DataTranferObjects.Promotion.Promotion> resultWithRank = promotions
                        .Select(MapToPromotionItem)
                        .ToList();

                    if (band != null && band.DiscountPercentage > 0)
                    {
                        resultWithRank.Add(new DataTranferObjects.Promotion.Promotion
                        {
                            Id = null,
                            Code = $"MEMBERSHIP_{band.Name}",
                            Name = $"Ưu đãi thành viên {band.Name}",
                            DiscountValue = band.DiscountPercentage,
                            DiscountType = "PERCENTAGE",
                            MaxDiscountAmount = 0,
                            MinOrderAmount = 0,
                            UsageLimit = 0,
                            UsagePerCustomer = 0,
                            ValidFrom = now,
                            ValidTo = now.AddYears(10),
                            IsActive = true,
                            ApplicableItems = new List<DataTranferObjects.Promotion.PromotionApplicableItem>()
                        });
                    }

                    return resultWithRank;
                }
            }

            return promotions.Select(MapToPromotionItem).ToList();
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
            await ValidatePromotionData(item);

            Models.Promotions.Promotion promotion;
            string normalizedCode = item.Code.Trim().ToUpperInvariant();
            string normalizedName = item.Name.Trim();
            string normalizedDiscountType = item.DiscountType.Trim().ToUpperInvariant();

            if (!item.Id.HasValue || item.Id == Guid.Empty)
            {
                bool codeExists = await Repo.GetExistsAsync<Models.Promotions.Promotion>(x => x.Code == normalizedCode);
                if (codeExists)
                {
                    throw new AppException("Promotion code already exists");
                }

                promotion = mapper.Map<Models.Promotions.Promotion>(item);
                promotion.Code = normalizedCode;
                promotion.Name = normalizedName;
                promotion.DiscountType = normalizedDiscountType;

                promotion.PromotionApplicableItems = item.ApplicableItems
                    .Select(x => new Models.Promotions.PromotionApplicableItem
                    {
                        DishId = x.DishId,
                        CategoryId = x.CategoryId,
                        ComboId = x.ComboId
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
                    x => x.Code == normalizedCode && x.Id != promotion.Id
                );
                if (codeExists)
                {
                    throw new AppException("Promotion code already exists");
                }

                promotion.Code = normalizedCode;
                promotion.Name = normalizedName;
                promotion.DiscountValue = item.DiscountValue;
                promotion.DiscountType = normalizedDiscountType;
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

        private async Task ValidatePromotionData(DataTranferObjects.Promotion.Promotion item)
        {
            if (item == null)
            {
                throw new AppException("Promotion data is required");
            }

            if (string.IsNullOrWhiteSpace(item.Code) || item.Code.Trim().Length > 20)
            {
                throw new AppException("Promotion code is required and must be <= 20 characters");
            }

            if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 255)
            {
                throw new AppException("Promotion name is required and must be <= 255 characters");
            }

            if (item.ValidTo < item.ValidFrom)
            {
                throw new AppException("ValidTo must be greater than or equal to ValidFrom");
            }

            if (item.DiscountValue < 0 || item.MaxDiscountAmount < 0 || item.MinOrderAmount < 0)
            {
                throw new AppException("Discount values cannot be negative");
            }

            if (item.UsageLimit < 0 || item.UsagePerCustomer < 0)
            {
                throw new AppException("UsageLimit and UsagePerCustomer cannot be negative");
            }

            if (item.UsageLimit > 0 && item.UsagePerCustomer > item.UsageLimit)
            {
                throw new AppException("UsagePerCustomer cannot be greater than UsageLimit");
            }

            string discountType = (item.DiscountType ?? string.Empty).Trim().ToUpperInvariant();
            if (discountType != "PERCENTAGE" && discountType != "FIXED")
            {
                throw new AppException("DiscountType must be PERCENTAGE or FIXED");
            }

            if (discountType == "PERCENTAGE" && (item.DiscountValue <= 0 || item.DiscountValue > 100))
            {
                throw new AppException("Percentage discount must be in range (0, 100]");
            }

            if (discountType == "FIXED" && item.DiscountValue <= 0)
            {
                throw new AppException("Fixed discount must be greater than 0");
            }

            List<DataTranferObjects.Promotion.PromotionApplicableItem> applicableItems = item.ApplicableItems ?? new List<DataTranferObjects.Promotion.PromotionApplicableItem>();
            foreach (DataTranferObjects.Promotion.PromotionApplicableItem ai in applicableItems)
            {
                int targetCount = 0;
                if (ai.DishId.HasValue) targetCount++;
                if (ai.CategoryId.HasValue) targetCount++;
                if (ai.ComboId.HasValue) targetCount++;

                if (targetCount != 1)
                {
                    throw new AppException("Each applicable item must target exactly one of DishId, CategoryId, ComboId");
                }

                if (ai.DishId.HasValue)
                {
                    bool dishExists = await Repo.GetExistsAsync<RestX.Models.Menu.Dish>(d => d.Id == ai.DishId.Value);
                    if (!dishExists) throw new AppException($"Dish '{ai.DishId}' not found");
                }

                if (ai.CategoryId.HasValue)
                {
                    bool categoryExists = await Repo.GetExistsAsync<RestX.Models.Menu.Category>(c => c.Id == ai.CategoryId.Value);
                    if (!categoryExists) throw new AppException($"Category '{ai.CategoryId}' not found");
                }

                if (ai.ComboId.HasValue)
                {
                    bool comboExists = await Repo.GetExistsAsync<RestX.Models.Menu.MealCombo>(c => c.Id == ai.ComboId.Value);
                    if (!comboExists) throw new AppException($"Combo '{ai.ComboId}' not found");
                }
            }
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
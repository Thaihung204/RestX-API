using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using RestX.Models.Loyalty;

namespace RestX.BLL.DataSeeders
{

    public class LoyaltySeeder
    {
        private readonly TenantDbContext context;

        public LoyaltySeeder(TenantDbContext context)
        {
            this.context = context;
        }

        public async Task SeedAsync()
        {
            if (await context.LoyaltyPointBands.AnyAsync())
            {
                return;
            }

            var bands = new List<LoyaltyPointBand>
            {
                new LoyaltyPointBand
                {
                    Id = Guid.NewGuid(),
                    Name = "Bronze",
                    Min = 0,
                    Max = 1000,
                    DiscountPercentage = 0,
                    BenefitDescription = "Thành viên cơ bản - Tích điểm cho mọi giao dịch",
                    IsActive = true
                },
                new LoyaltyPointBand
                {
                    Id = Guid.NewGuid(),
                    Name = "Silver",
                    Min = 1001,
                    Max = 5000,
                    DiscountPercentage = 5,
                    BenefitDescription = "Giảm 5% cho mọi đơn hàng - Ưu đãi sinh nhật",
                    IsActive = true
                },
                new LoyaltyPointBand
                {
                    Id = Guid.NewGuid(),
                    Name = "Gold",
                    Min = 5001,
                    Max = 10000,
                    DiscountPercentage = 10,
                    BenefitDescription = "Giảm 10% cho mọi đơn hàng - Ưu tiên đặt bàn - Voucher sinh nhật",
                    IsActive = true
                },
                new LoyaltyPointBand
                {
                    Id = Guid.NewGuid(),
                    Name = "Platinum",
                    Min = 10001,
                    Max = null, 
                    DiscountPercentage = 15,
                    BenefitDescription = "Giảm 15% cho mọi đơn hàng - VIP treatment - Phòng riêng - Quà sinh nhật cao cấp",
                    IsActive = true
                }
            };

            await context.LoyaltyPointBands.AddRangeAsync(bands);
            await context.SaveChangesAsync();
        }
    }
}

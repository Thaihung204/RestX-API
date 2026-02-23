using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using RestX.Models.Loyalty;
using Serilog;

namespace RestX.DAL.DataSeeders
{
    public class LoyaltySeeder : IDataSeeder
    {
        private readonly TenantDbContext context;
        public LoyaltySeeder(TenantDbContext context)
        {
            this.context = context;
        }
        public int Order => 5;
        public async Task SeedAsync()
        {
            Log.Information("[LoyaltySeeder] Seeding loyalty point bands...");
            if (await context.LoyaltyPointBands.AnyAsync())
            {
                Log.Information("[LoyaltySeeder] Loyalty bands already exist, skipping...");
                return;
            }
            var bands = CreateLoyaltyBands();
            await context.LoyaltyPointBands.AddRangeAsync(bands);
            await context.SaveChangesAsync();
            Log.Information("[LoyaltySeeder] Loyalty point bands seeded successfully");
        }
        private static List<LoyaltyPointBand> CreateLoyaltyBands()
        {
            return new List<LoyaltyPointBand>
    {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Bronze",
                    Min = 0,
                    Max = 999,
                    DiscountPercentage = 0,
                    BenefitDescription = "Thành viên cơ bản - Tích điểm cho mọi giao dịch",
                    LogoColor = "#CD7F32",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Silver",
                    Min = 1000,
                    Max = 4999,
                    DiscountPercentage = 3,
                    BenefitDescription = "Giảm 3% cho mọi đơn hàng - Ưu đãi sinh nhật",
                    LogoColor = "#C0C0C0",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Gold",
                    Min = 5000,
                    Max = 14999,
                    DiscountPercentage = 7,
                    BenefitDescription = "Giảm 7% cho mọi đơn hàng - Ưu tiên đặt bàn - Voucher sinh nhật",
                    LogoColor = "#FFD700",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Platinum",
                    Min = 15000,
                    Max = 29999,
                    DiscountPercentage = 10,
                    BenefitDescription = "Giảm 10% - Ưu tiên đặt bàn - Quà sinh nhật cao cấp - Hỗ trợ đặt phòng riêng",
                    LogoColor = "#E5E4E2",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Diamond",
                    Min = 30000,
                    Max = null,
                    DiscountPercentage = 12,
                    BenefitDescription = "Giảm 12% - VIP treatment - Phòng riêng miễn phí - Quà sinh nhật đặc biệt - Ưu đãi sự kiện riêng",
                    LogoColor = "#B9F2FF",
                    IsActive = true
                }
            };
        }

    }
}

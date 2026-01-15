//using Microsoft.EntityFrameworkCore;
//using RestX.DAL.Context;
//using RestX.Models.Menu;
//using RestX.Models.Tables;

//namespace RestX.BLL.DataSeeders
//{

//    public class MasterDataSeeder
//    {
//        private readonly TenantDbContext _context;

//        public MasterDataSeeder(TenantDbContext context)
//        {
//            _context = context;
//        }

//        public async Task SeedAsync()
//        {
//            await SeedCategoriesAsync();
//            await SeedSampleTablesAsync();
//        }

//        private async Task SeedCategoriesAsync()
//        {
//            if (await _context.Categories.AnyAsync())
//            {
//                return;
//            }

//            var categories = new List<Category>
//            {
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Appetizers",
//                    Description = "Món khai vị - Starters and appetizers",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Soups",
//                    Description = "Súp và canh - Soups and broths",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Salads",
//                    Description = "Salad - Fresh salads",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Main Course",
//                    Description = "Món chính - Main dishes",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Seafood",
//                    Description = "Hải sản - Seafood dishes",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Meat Dishes",
//                    Description = "Món thịt - Meat dishes",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Vegetarian",
//                    Description = "Món chay - Vegetarian dishes",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Beverages",
//                    Description = "Đồ uống - Beverages",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Soft Drinks",
//                    Description = "Nước ngọt - Soft drinks",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Hot Drinks",
//                    Description = "Đồ uống nóng - Hot beverages",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Alcoholic Drinks",
//                    Description = "Đồ uống có cồn - Alcoholic beverages",
//                    IsActive = true
//                },
//                new Category
//                {
//                    Id = Guid.NewGuid(),
//                    Name = "Desserts",
//                    Description = "Tráng miệng - Desserts",
//                    IsActive = true
//                }
//            };

//            await _context.Categories.AddRangeAsync(categories);
//            await _context.SaveChangesAsync();
//        }

//        private async Task SeedSampleTablesAsync()
//        {
//            if (await _context.Tables.AnyAsync())
//            {
//                return;
//            }

//            var availableStatus = await _context.StatusValues
//                .FirstOrDefaultAsync(sv => sv.StatusType.Code == "TABLE_STATUS" && sv.Code == "AVAILABLE");

//            if (availableStatus == null)
//            {
//                return;
//            }

//            var tables = new List<Table>
//            {
//                CreateTable("T01", "Regular", 2, availableStatus.Id),
//                CreateTable("T02", "Regular", 2, availableStatus.Id),
//                CreateTable("T03", "Regular", 4, availableStatus.Id),
//                CreateTable("T04", "Regular", 4, availableStatus.Id),
//                CreateTable("T05", "Regular", 6, availableStatus.Id),
//                CreateTable("T06", "Regular", 6, availableStatus.Id),
//                CreateTable("T07", "Large", 8, availableStatus.Id),
//                CreateTable("T08", "Large", 8, availableStatus.Id),
//                CreateTable("VIP01", "VIP", 10, availableStatus.Id),
//                CreateTable("VIP02", "VIP", 12, availableStatus.Id)
//            };

//            await _context.Tables.AddRangeAsync(tables);
//            await _context.SaveChangesAsync();
//        }

//        private Table CreateTable(string code, string type, int seatingCapacity, Guid statusId)
//        {
//            return new Table
//            {
//                Id = Guid.NewGuid(),
//                Code = code,
//                Type = type,
//                SeatingCapacity = seatingCapacity,
//                TableStatusId = statusId,
//                IsActive = true,
//                Shape = "Rectangle",
//                PositionX = 0,
//                PositionY = 0,
//                Width = type == "VIP" ? 200 : 150,
//                Height = type == "VIP" ? 200 : 150,
//                Rotation = 0
//            };
//        }
//    }
//}

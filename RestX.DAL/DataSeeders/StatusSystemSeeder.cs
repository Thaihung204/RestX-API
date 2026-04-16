using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using RestX.Models.Common;
using Serilog;

namespace RestX.DAL.DataSeeders
{
    public class StatusSystemSeeder : IDataSeeder
    {
        private readonly TenantDbContext context;
        public StatusSystemSeeder(TenantDbContext context)
        {
            this.context = context;
        }
        public int Order => 1;
        public async Task SeedAsync()
        {
            Log.Information("[StatusSystemSeeder] Seeding status types and values...");
            if (await context.StatusTypes.AnyAsync())
            {
                Log.Information("[StatusSystemSeeder] Status data already exists, skipping...");
                return;
            }
            var statusTypes = CreateStatusTypes();
            await context.StatusTypes.AddRangeAsync(statusTypes);
            await context.SaveChangesAsync();
            var statusValues = CreateStatusValues(statusTypes);
            await context.StatusValues.AddRangeAsync(statusValues);
            await context.SaveChangesAsync();
            Log.Information("[StatusSystemSeeder] Status types and values seeded successfully");
        }
        private static List<StatusType> CreateStatusTypes()
        {
            return new List<StatusType>
            {
                new() { Code = "RESERVATION" },
                new() { Code = "ORDER-DETAIL" }
            };
        }
        private static List<StatusValue> CreateStatusValues(List<StatusType> statusTypes)
        {
            var values = new List<StatusValue>();
            var typeMap = statusTypes.ToDictionary(t => t.Code, t => t.Id);
            values.AddRange(CreateValuesForType(typeMap["RESERVATION"], new[]
            {
                ("PENDING", "Pending", "#FF9800", true, 1),
                ("CONFIRMED", "Confirmed", "#4CAF50", false, 2),
                ("CANCELLED", "Cancelled", "#F44336", false, 3)
            }));
            values.AddRange(CreateValuesForType(typeMap["ORDER-DETAIL"], new[]
            {
                ("PREPARING", "Preparing", "#2196F3", true, 1),
                ("SERVED", "Served", "#9C27B0", false, 2),
                ("CANCELLED", "Cancelled", "#F44336", false, 3)
            }));
            return values;
        }
        private static IEnumerable<StatusValue> CreateValuesForType(
            int typeId,
            (string Code, string Name, string Color, bool IsDefault, int DisplayOrder)[] definitions)
        {
            return definitions.Select(d => new StatusValue
            {
                StatusTypeId = typeId,
                Code = d.Code,
                Name = d.Name,
                ColorCode = d.Color,
                IsDefault = d.IsDefault,
                DisplayOrder = d.DisplayOrder,
                IsSystem = true
            });
        }
    }
}
using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using RestX.Models.Common;
using RestX.Models.Enum;
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
                new() { Code = "ORDER" },
                new() { Code = "PAYMENT" },
                new() { Code = "RESERVATION" },
                new() { Code = "TABLE" },
                new() { Code = "ORDER-DETAIL" }
            };
        }
        private static List<StatusValue> CreateStatusValues(List<StatusType> statusTypes)
        {
            var values = new List<StatusValue>();
            var typeMap = statusTypes.ToDictionary(t => t.Code, t => t.Id);
            values.AddRange(CreateValuesForType(typeMap["ORDER"], new[]
            {
                (nameof(OrderStatus.Reserved), "Reserved", "#FFA500", true),
                (nameof(OrderStatus.Serving), "Serving", "#9C27B0", false),
                (nameof(OrderStatus.Completed), "Completed", "#4CAF50", false),
                (nameof(OrderStatus.Cancelled), "Cancelled", "#F44336", false)
            }));
            values.AddRange(CreateValuesForType(typeMap["TABLE_STATUS"], new[]
            {
                (nameof(TableStatus.Available), "Available", "#4CAF50", true),
                (nameof(TableStatus.Reserved), "Reserved", "#FF9800", false),
                (nameof(TableStatus.Occupied), "Occupied", "#F44336", false)
            }));
            values.AddRange(CreateValuesForType(typeMap["PAYMENT"], new[]
            {
                ("UNPAID", "Unpaid", "#FF9800", true),
                ("PAID", "Paid", "#4CAF50", false),
                ("CANCELLED", "Cancelled", "#F44336", false),
                ("REFUNDED", "Refunded", "#9E9E9E", false)
            }));
            values.AddRange(CreateValuesForType(typeMap["RESERVATION"], new[]
            {
                ("PENDING", "Pending", "#FFA500", true),
                ("CONFIRMED", "Confirmed", "#4CAF50", false),
                ("COMPLETED", "Completed", "#00C853", false),
                ("CANCELLED", "Cancelled", "#F44336", false)
            }));
            values.AddRange(CreateValuesForType(typeMap["ORDER-DETAIL"], new[]
            {
                ("PENDING", "Pending", "#FFA500", true),
                ("PREPARING", "Preparing", "#2196F3", false),
                ("READY", "Ready", "#00C853", false),
                ("SERVED", "Served", "#9C27B0", false),
                ("CANCELLED", "Cancelled", "#F44336", false)
            }));
            return values;
        }
        private static IEnumerable<StatusValue> CreateValuesForType(
            int typeId,
            (string Code, string Name, string Color, bool IsDefault)[] definitions)
        {
            return definitions.Select(d => new StatusValue
            {
                StatusTypeId = typeId,
                Code = d.Code,
                Name = d.Name,
                ColorCode = d.Color,
                IsDefault = d.IsDefault
            });
        }
    }
}
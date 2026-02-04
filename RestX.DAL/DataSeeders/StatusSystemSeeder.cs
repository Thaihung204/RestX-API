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
                new() { Id = Guid.NewGuid(), Code = "ORDER_STATUS" },
                new() { Id = Guid.NewGuid(), Code = "PAYMENT_STATUS" },
                new() { Id = Guid.NewGuid(), Code = "RESERVATION_STATUS" },
                new() { Id = Guid.NewGuid(), Code = "TABLE_STATUS" },
                new() { Id = Guid.NewGuid(), Code = "ITEM_STATUS" }
            };
        }
        private static List<StatusValue> CreateStatusValues(List<StatusType> statusTypes)
        {
            var values = new List<StatusValue>();
            var typeMap = statusTypes.ToDictionary(t => t.Code, t => t.Id);
            values.AddRange(CreateValuesForType(typeMap["ORDER_STATUS"], new[]
            {
                ("RESERVED", "Reserved", "#FFA500"),
                ("SERVING", "Serving", "#9C27B0"),
                ("COMPLETED", "Completed", "#4CAF50"),
                ("DELETED", "Deleted", "#F44336")
            }));

            values.AddRange(CreateValuesForType(typeMap["PAYMENT_STATUS"], new[]
            {
                ("UNPAID", "Unpaid", "#FF9800"),
                ("PAID", "Paid", "#4CAF50"),
                ("REFUNDED", "Refunded", "#9E9E9E")
            }));

            values.AddRange(CreateValuesForType(typeMap["RESERVATION_STATUS"], new[]
            {
                ("PENDING", "Pending", "#FFA500"),
                ("CONFIRMED", "Confirmed", "#4CAF50"),
                ("COMPLETED", "Completed", "#00C853"),
                ("CANCELLED", "Cancelled", "#F44336")
            }));

            values.AddRange(CreateValuesForType(typeMap["TABLE_STATUS"], new[]
            {
                ("AVAILABLE", "Available", "#4CAF50"),
                ("RESERVED", "Reserved", "#FF9800"),
                ("OCCUPIED", "Occupied", "#F44336")
            }));

            values.AddRange(CreateValuesForType(typeMap["ITEM_STATUS"], new[]
            {
                ("PENDING", "Pending", "#FFA500"),
                ("PREPARING", "Preparing", "#2196F3"),
                ("READY", "Ready", "#00C853"),
                ("SERVED", "Served", "#9C27B0"),
                ("CANCELLED", "Cancelled", "#F44336")
            }));

            return values;
        }
        private static IEnumerable<StatusValue> CreateValuesForType(
            Guid typeId,
            (string Code, string Name, string Color)[] definitions)
        {
            return definitions.Select(d => new StatusValue
            {
                Id = Guid.NewGuid(),
                StatusTypeId = typeId,
                Code = d.Code,
                Name = d.Name,
                ColorCode = d.Color,
                IsActive = true
            });
        }
    }
}

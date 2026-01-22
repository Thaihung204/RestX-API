using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using RestX.Models.Common;

namespace RestX.BLL.DataSeeders
{

    public class StatusSystemSeeder
    {
        private readonly TenantDbContext context;

        public StatusSystemSeeder(TenantDbContext context)
        {
            this.context = context;
        }

        public async Task SeedAsync()
        {
            if (await context.StatusTypes.AnyAsync())
            {
                return;
            }

            var statusTypes = CreateStatusTypes();
            await context.StatusTypes.AddRangeAsync(statusTypes);
            await context.SaveChangesAsync();

            var statusValues = CreateStatusValues(statusTypes);
            await context.StatusValues.AddRangeAsync(statusValues);
            await context.SaveChangesAsync();
        }

        private List<StatusType> CreateStatusTypes()
        {
            return new List<StatusType>
            {
                new StatusType { Id = Guid.NewGuid(), Code = "ORDER_STATUS" },
                new StatusType { Id = Guid.NewGuid(), Code = "PAYMENT_STATUS" },
                new StatusType { Id = Guid.NewGuid(), Code = "RESERVATION_STATUS" },
                new StatusType { Id = Guid.NewGuid(), Code = "TABLE_STATUS" },
                new StatusType { Id = Guid.NewGuid(), Code = "ITEM_STATUS" },
                new StatusType { Id = Guid.NewGuid(), Code = "EMPLOYEE_STATUS" }
            };
        }

        private List<StatusValue> CreateStatusValues(List<StatusType> statusTypes)
        {
            var statusValues = new List<StatusValue>();

            var orderStatusType = statusTypes.First(st => st.Code == "ORDER_STATUS");
            statusValues.AddRange(new[]
            {
                CreateStatusValue(orderStatusType.Id, "PENDING", "Pending", "#FFA500"),
                CreateStatusValue(orderStatusType.Id, "CONFIRMED", "Confirmed", "#4CAF50"),
                CreateStatusValue(orderStatusType.Id, "PREPARING", "Preparing", "#2196F3"),
                CreateStatusValue(orderStatusType.Id, "READY", "Ready", "#00C853"),
                CreateStatusValue(orderStatusType.Id, "SERVING", "Serving", "#9C27B0"),
                CreateStatusValue(orderStatusType.Id, "COMPLETED", "Completed", "#4CAF50"),
                CreateStatusValue(orderStatusType.Id, "CANCELLED", "Cancelled", "#F44336")
            });

            var paymentStatusType = statusTypes.First(st => st.Code == "PAYMENT_STATUS");
            statusValues.AddRange(new[]
            {
                CreateStatusValue(paymentStatusType.Id, "UNPAID", "Unpaid", "#FF9800"),
                CreateStatusValue(paymentStatusType.Id, "PARTIAL_PAID", "Partial Paid", "#FFC107"),
                CreateStatusValue(paymentStatusType.Id, "PAID", "Paid", "#4CAF50"),
                CreateStatusValue(paymentStatusType.Id, "REFUNDED", "Refunded", "#9E9E9E")
            });

            var reservationStatusType = statusTypes.First(st => st.Code == "RESERVATION_STATUS");
            statusValues.AddRange(new[]
            {
                CreateStatusValue(reservationStatusType.Id, "PENDING", "Pending", "#FFA500"),
                CreateStatusValue(reservationStatusType.Id, "CONFIRMED", "Confirmed", "#4CAF50"),
                CreateStatusValue(reservationStatusType.Id, "ARRIVED", "Arrived", "#2196F3"),
                CreateStatusValue(reservationStatusType.Id, "SEATED", "Seated", "#9C27B0"),
                CreateStatusValue(reservationStatusType.Id, "COMPLETED", "Completed", "#00C853"),
                CreateStatusValue(reservationStatusType.Id, "CANCELLED", "Cancelled", "#F44336"),
                CreateStatusValue(reservationStatusType.Id, "NO_SHOW", "No Show", "#D32F2F")
            });

            var tableStatusType = statusTypes.First(st => st.Code == "TABLE_STATUS");
            statusValues.AddRange(new[]
            {
                CreateStatusValue(tableStatusType.Id, "AVAILABLE", "Available", "#4CAF50"),
                CreateStatusValue(tableStatusType.Id, "OCCUPIED", "Occupied", "#F44336"),
                CreateStatusValue(tableStatusType.Id, "RESERVED", "Reserved", "#FF9800"),
                CreateStatusValue(tableStatusType.Id, "CLEANING", "Cleaning", "#2196F3"),
                CreateStatusValue(tableStatusType.Id, "MAINTENANCE", "Maintenance", "#9E9E9E")
            });

            var itemStatusType = statusTypes.First(st => st.Code == "ITEM_STATUS");
            statusValues.AddRange(new[]
            {
                CreateStatusValue(itemStatusType.Id, "PENDING", "Pending", "#FFA500"),
                CreateStatusValue(itemStatusType.Id, "PREPARING", "Preparing", "#2196F3"),
                CreateStatusValue(itemStatusType.Id, "READY", "Ready", "#00C853"),
                CreateStatusValue(itemStatusType.Id, "SERVED", "Served", "#9C27B0"),
                CreateStatusValue(itemStatusType.Id, "CANCELLED", "Cancelled", "#F44336")
            });

            var employeeStatusType = statusTypes.First(st => st.Code == "EMPLOYEE_STATUS");
            statusValues.AddRange(new[]
            {
                CreateStatusValue(employeeStatusType.Id, "ON_DUTY", "On Duty", "#4CAF50"),
                CreateStatusValue(employeeStatusType.Id, "OFF_DUTY", "Off Duty", "#9E9E9E"),
                CreateStatusValue(employeeStatusType.Id, "ON_BREAK", "On Break", "#FFC107"),
                CreateStatusValue(employeeStatusType.Id, "LEAVE", "Leave", "#FF9800")
            });

            return statusValues;
        }

        private StatusValue CreateStatusValue(Guid statusTypeId, string code, string name, string colorCode)
        {
            return new StatusValue
            {
                Id = Guid.NewGuid(),
                StatusTypeId = statusTypeId,
                Code = code,
                Name = name,
                ColorCode = colorCode,
                IsActive = true
            };
        }
    }
}

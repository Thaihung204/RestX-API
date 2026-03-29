using AutoMapper;
using Microsoft.Data.SqlClient;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Inventory;
using RestX.BLL.Interfaces.Status;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Common;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Orders;
using RestX.Models.Tenants;
using System.Data;
using System.Text;

namespace RestX.BLL.Services
{
    public class OrderService : BaseService, IOrderService
    {
        private readonly IMapper mapper;
        private readonly IStatusValueService statusValueService;
        private readonly ITableService tableService;
        private readonly IIngredientService ingredientService;

        public OrderService(
            IIngredientService ingredientService,
            IStatusValueService statusValueService,
            ITableService tableService,
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.ingredientService = ingredientService;
            this.statusValueService = statusValueService;
            this.tableService = tableService;
            this.mapper = mapper;
        }

        public async Task<OrderSearchResult> GetAllOrders(OrderSearch model)
        {
            if (model.Page <= 0) model.Page = 1;
            if (model.ItemsPerPage <= 0) model.ItemsPerPage = 20;
            if (model.ItemsPerPage > 200) model.ItemsPerPage = 200;

            var result = new OrderSearchResult
            {
                Page = model.Page,
                ItemsPerPage = model.ItemsPerPage
            };

            var query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM dbo.Orders o
                WHERE 1 = 1
            ");

            var countParams = new List<SqlParameter>();
            var queryParams = new List<SqlParameter>();

            if (model.Status.HasValue)
            {
                query.Append(" AND o.OrderStatusId = @Status ");

                var statusValue = (int)model.Status.Value;
                countParams.Add(new SqlParameter("Status", SqlDbType.Int) { Value = statusValue });
                queryParams.Add(new SqlParameter("Status", SqlDbType.Int) { Value = statusValue });
            }

            if (model.From.HasValue)
            {
                var fromUtc = model.From.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(model.From.Value, DateTimeKind.Utc)
                    : model.From.Value.ToUniversalTime();

                query.Append(" AND o.CreatedDate >= @From ");

                countParams.Add(new SqlParameter("From", SqlDbType.DateTime2) { Value = fromUtc });
                queryParams.Add(new SqlParameter("From", SqlDbType.DateTime2) { Value = fromUtc });
            }

            if (model.To.HasValue)
            {
                var toUtcExclusive = (model.To.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(model.To.Value, DateTimeKind.Utc)
                        : model.To.Value.ToUniversalTime())
                    .Date
                    .AddDays(1);

                query.Append(" AND o.CreatedDate < @ToExclusive ");

                countParams.Add(new SqlParameter("ToExclusive", SqlDbType.DateTime2) { Value = toUtcExclusive });
                queryParams.Add(new SqlParameter("ToExclusive", SqlDbType.DateTime2) { Value = toUtcExclusive });
            }

            var countQuery = query.ToString().Replace("#SELECT#", "COUNT(1)");
            result.TotalCount = await Repo.ExecuteSqlCommandAsync<int>(
                countQuery,
                countParams.Any() ? countParams.Cast<object>().ToArray() : null
            );

            result.TotalPages = result.ItemsPerPage <= 0
                ? 0
                : (int)Math.Ceiling((decimal)result.TotalCount / result.ItemsPerPage);

            var skip = result.Page <= 1 ? 0 : (result.Page - 1) * result.ItemsPerPage;

            var selectItems = @"
                o.Id,
                o.Reference,
                o.CustomerId,
                o.ReservationId,
                o.OrderStatusId,
                o.SubTotal,
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceCharge,
                o.TotalAmount,
                o.CompletedAt,
                o.CancelledAt,
                o.HandledBy,
                o.CreatedDate
            ";

            var mainQuery = query.ToString().Replace("#SELECT#", selectItems);

            mainQuery += (model.SortBy?.ToLowerInvariant()) switch
            {
                "created_asc" => " ORDER BY o.CreatedDate ASC",
                _ => " ORDER BY o.CreatedDate DESC"
            };

            mainQuery += $" OFFSET {skip} ROWS FETCH NEXT {result.ItemsPerPage} ROWS ONLY";

            var orders = await Repo.ExecuteSqlSelectAsync<Models.Orders.Order>(
                mainQuery,
                queryParams.Any() ? queryParams.Cast<object>().ToArray() : null
            );

            if (orders.Count == 0)
            {
                result.Orders = new List<DataTranferObjects.Orders.Order>();
                return result;
            }

            var orderIds = orders.Select(o => o.Id).ToList();
            var idParams = orderIds
                .Select((id, i) => new SqlParameter($"OrderId{i}", SqlDbType.UniqueIdentifier) { Value = id })
                .ToList();

            var inClause = string.Join(", ", idParams.Select(p => "@" + p.ParameterName));

            var orderTablesQuery = $@"
                SELECT
                    ot.Id,
                    ot.OrderId,
                    ot.TableId
                FROM dbo.OrderTables ot
                WHERE ot.OrderId IN ({inClause})
                ORDER BY ot.OrderId, ot.Id
            ";

            var orderDetailsQuery = $@"
                SELECT
                    od.Id,
                    od.OrderId,
                    od.DishId,
                    od.Quantity,
                    od.Note,
                    od.ItemStatusId
                FROM dbo.OrderDetails od
                WHERE od.OrderId IN ({inClause})
                ORDER BY od.OrderId, od.Id
            ";

            var itemStatusesQuery = $@"
                SELECT
                    sv.Id,
                    sv.Name
                FROM dbo.StatusValues sv
                WHERE sv.Id IN (
                    SELECT DISTINCT od.ItemStatusId
                    FROM dbo.OrderDetails od
                    WHERE od.OrderId IN ({inClause})
                )
            ";

            var orderTables = await Repo.ExecuteSqlSelectAsync<OrderTable>(
                orderTablesQuery,
                CloneParams(idParams)
            );

            var orderDetails = await Repo.ExecuteSqlSelectAsync<Models.Orders.OrderDetail>(
                orderDetailsQuery,
                CloneParams(idParams)
            );

            var itemStatuses = await Repo.ExecuteSqlSelectAsync<StatusValue>(
                itemStatusesQuery,
                CloneParams(idParams)
            );

            var tablesByOrderId = orderTables
                .GroupBy(t => t.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var detailsByOrderId = orderDetails
                .GroupBy(d => d.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var statusById = itemStatuses
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var paidPayments = await Repo.GetAsync<Payment>(
                p => p.OrderId.HasValue && orderIds.Contains(p.OrderId.Value) && p.Status == PaymentStatus.Success);
            var paidOrderIds = paidPayments.Select(p => p.OrderId!.Value).ToHashSet();

            foreach (var o in orders)
            {
                if (tablesByOrderId.TryGetValue(o.Id, out var ots))
                    o.OrderTables = ots;

                if (detailsByOrderId.TryGetValue(o.Id, out var ods))
                {
                    foreach (var d in ods)
                    {
                        if (statusById.TryGetValue(d.ItemStatusId, out var s))
                            d.ItemStatus = s;
                    }

                    o.OrderDetails = ods;
                }

                o.IsPaid = paidOrderIds.Contains(o.Id);
            }

            result.Orders = mapper.Map<List<DataTranferObjects.Orders.Order>>(orders);
            return result;
        }
        private static object[] CloneParams(IEnumerable<SqlParameter> parameters)
            => parameters
                .Select(p => new SqlParameter(p.ParameterName, p.SqlDbType)
                {
                    Value = p.Value ?? DBNull.Value,
                    Direction = p.Direction,
                    Size = p.Size,
                    Precision = p.Precision,
                    Scale = p.Scale,
                    IsNullable = p.IsNullable
                })
                .Cast<object>()
                .ToArray();


        public async Task<DataTranferObjects.Orders.Order?> GetOrderById(Guid id)
        {
            var order = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == id,
                includeProperties: "OrderDetails,OrderDetails.ItemStatus,OrderTables,Payments"
            );

            return mapper.Map<DataTranferObjects.Orders.Order>(order);
        }

        public async Task<Guid> CreateOrder(DataTranferObjects.Orders.Order order, string userId)
        {
            var dishIds = order.OrderDetails
                .Select(x => x.DishId)
                .Distinct()
                .ToList();

            var dishes = (await Repo.GetAsync<Dish>(filter: d => dishIds.Contains(d.Id))).ToList();
            var dishesById = dishes.ToDictionary(d => d.Id, d => d);

            var discountAmount = order.DiscountAmount ?? 0m;
            var taxAmount = order.TaxAmount ?? 0m;
            var serviceCharge = order.ServiceCharge ?? 0m;

            var subTotal = order.OrderDetails.Sum(x => x.Quantity * dishesById[x.DishId].Price);

            var itemStatuses = (await statusValueService.GetStatuses("order-detail")).ToList();

            var orderEntity = new Models.Orders.Order
            {
                Reference = await GetNextOrderReference(),

                CustomerId = order.CustomerId,
                ReservationId = order.ReservationId,

                OrderStatusId = order.OrderStatusId,

                DiscountAmount = discountAmount,
                TaxAmount = taxAmount,
                ServiceCharge = serviceCharge,

                SubTotal = subTotal,

                OrderTables = new List<OrderTable>
                {
                    new OrderTable { TableId = order.TableId }
                },

                OrderDetails = order.OrderDetails.Select(d => new Models.Orders.OrderDetail
                {
                    DishId = d.DishId,
                    Quantity = d.Quantity,
                    Note = d.Note,
                    ItemStatusId = itemStatuses.First(x => x.IsDefault).Id,
                }).ToList()
            };

            orderEntity.CalculateTotalAmount();

            await Repo.CreateAsync(orderEntity, userId);
            await tableService.ChangeTableStatus(order.TableId, TableStatus.Reserved);

            return orderEntity.Id;
        }

        public async Task<Guid> UpdateOrder(Guid id, DataTranferObjects.Orders.Order orderDto, string userId)
        {
            var orderEntity = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == id,
                includeProperties: "OrderDetails,OrderTables"
            );

            if (orderEntity == null)
                return Guid.Empty;

            var dishIds = (orderDto.OrderDetails ?? new List<DataTranferObjects.Orders.OrderDetail>())
                .Select(x => x.DishId)
                .Distinct()
                .ToList();

            var dishes = (await Repo.GetAsync<Dish>(filter: d => dishIds.Contains(d.Id))).ToList();
            var dishesById = dishes.ToDictionary(d => d.Id, d => d);

            var discountAmount = orderDto.DiscountAmount ?? 0m;
            var taxAmount = orderDto.TaxAmount ?? 0m;
            var serviceCharge = orderDto.ServiceCharge ?? 0m;

            var subTotal = (orderDto.OrderDetails?.Sum(x => x.Quantity * dishesById[x.DishId].Price)) ?? 0m;

            var itemStatuses = (await statusValueService.GetStatuses("order-detail")).ToList();

            orderEntity.Reference = orderDto.Reference ?? orderEntity.Reference;
            orderEntity.CustomerId = orderDto.CustomerId;
            orderEntity.ReservationId = orderDto.ReservationId;

            orderEntity.DiscountAmount = discountAmount;
            orderEntity.TaxAmount = taxAmount;
            orderEntity.ServiceCharge = serviceCharge;

            orderEntity.CompletedAt = orderDto.CompletedAt;
            orderEntity.CancelledAt = orderDto.CancelledAt;
            orderEntity.HandledBy = orderDto.HandledBy;

            if (orderEntity.OrderDetails?.Any() == true)
            {
                foreach (var d in orderEntity.OrderDetails.ToList())
                    Repo.Delete<Models.Orders.OrderDetail>(d.Id);
            }

            if (orderDto.OrderDetails?.Any() == true)
            {
                foreach (var d in orderDto.OrderDetails)
                {
                    await Repo.CreateAsync(new Models.Orders.OrderDetail
                    {
                        OrderId = orderEntity.Id,
                        DishId = d.DishId,
                        Quantity = d.Quantity,
                        Note = d.Note,
                        ItemStatusId = itemStatuses.First(x => x.IsDefault).Id
                    });
                }
            }

            if (orderEntity.OrderTables?.Any() == true)
            {
                foreach (var ot in orderEntity.OrderTables.ToList())
                    Repo.Delete<OrderTable>(ot.Id);
            }

            var tableIds = (orderDto.TableIds ?? new List<Guid>())
                .Append(orderDto.TableId)
                .Distinct()
                .ToList();

            foreach (var tableId in tableIds)
            {
                await Repo.CreateAsync(new OrderTable
                {
                    OrderId = orderEntity.Id,
                    TableId = tableId
                });
            }

            orderEntity.SubTotal = subTotal;
            orderEntity.CalculateTotalAmount();

            Repo.Update(orderEntity, userId);
            await Repo.SaveAsync();

            return orderEntity.Id;
        }

        private async Task<string> GetNextOrderReference()
        {
            var tenantPrefix = CurrentTenant.Prefix;
            var reference = $"{tenantPrefix}{DateTime.UtcNow.AddHours(7):yMdsff}";

            var exists = await Repo.GetExistsAsync<Models.Orders.Order>(o => o.Reference == reference);
            var count = 0;

            while (exists && count < 20)
            {
                if (count < 1)
                {
                    reference = $"{tenantPrefix}{DateTime.UtcNow.AddHours(7):yMdsff}";
                }
                else if (count < 2)
                {
                    reference = $"{tenantPrefix}{DateTime.UtcNow.AddHours(7):yMdsfff}";
                }
                else if (count < 10)
                {
                    reference = $"{tenantPrefix}{DateTime.UtcNow.AddHours(7):yMdsHHfff}";
                }
                else
                {
                    reference = $"{tenantPrefix}{DateTime.UtcNow.AddHours(7):yMdsHHmmfff}";
                }

                exists = await Repo.GetExistsAsync<Models.Orders.Order>(o => o.Reference == reference);
                count++;
            }

            return reference;
        }

        public async Task DeleteOrder(Guid id)
        {
            var order = await Repo.GetByIdAsync<Models.Orders.Order>(id);
            if (order == null)
                return;

            Repo.Delete<Models.Orders.Order>(id);
            await Repo.SaveAsync();
        }

        public async Task<bool> UpdateStatus(Guid orderId, int statusId, string userId)
        {
            var order = await Repo.GetByIdAsync<Models.Orders.Order>(orderId);
            if (order == null)
                return false;

            var oldStatus = order.OrderStatusId;

            if (statusId == (int)OrderStatus.Completed)
            {
                var hasPaidPayment = await Repo.GetExistsAsync<Payment>(
                    p => p.OrderId == orderId && p.Status == PaymentStatus.Paid);
                if (!hasPaidPayment)
                    throw new AppException("Cannot complete order: order has not been paid");
            }

            if (userId != String.Empty)
            {
                order.HandledBy = Guid.Parse(userId);
            }
            order.OrderStatusId = (OrderStatus)statusId;

            if (statusId == (int)OrderStatus.Cancelled)
                order.CancelledAt = DateTime.UtcNow.AddHours(7);
            else if (statusId == (int)OrderStatus.Completed)
                order.CompletedAt = DateTime.UtcNow.AddHours(7);

            if (oldStatus != OrderStatus.Confirmed && statusId == (int)OrderStatus.Confirmed)
            {
                var orderDetails = await Repo.GetAsync<Models.Orders.OrderDetail>(
                    filter: od => od.OrderId == orderId,
                    includeProperties: "Dish"
                );

                foreach (var detail in orderDetails)
                {
                    var hasRecipe = await Repo.GetExistsAsync<DishRecipe>(r => r.DishId == detail.DishId);
                    if (hasRecipe)
                    {
                        await ingredientService.DeductFromRecipe(detail.DishId, detail.Quantity);
                    }
                }
            }

            Repo.Update(order, userId);
            await Repo.SaveAsync();

            return true;
        }

        public async Task<bool> UpdateOrderDetailStatus(Guid orderDetailId, int statusId, string userId)
        {
            var orderDetail = await Repo.GetByIdAsync<Models.Orders.OrderDetail>(orderDetailId);
            if (orderDetail == null)
                return false;

            orderDetail.ItemStatusId = statusId;

            Repo.Update(orderDetail, userId);
            await Repo.SaveAsync();

            return true;
        }
    }
}
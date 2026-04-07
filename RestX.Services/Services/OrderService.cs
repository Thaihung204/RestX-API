using AutoMapper;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Inventory;
using RestX.BLL.Interfaces.Status;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Common;
using RestX.Models.Customers;
using RestX.Models.Enum;
using RestX.Models.Loyalty;
using RestX.Models.Menu;
using RestX.Models.Orders;
using RestX.Models.Promotions;
using RestX.Models.Reservations;
using RestX.Models.Tables;
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

            var orderDetails = await Repo.ExecuteSqlSelectAsync<Models.Orders.OrderDetail>(
                orderDetailsQuery,
                CloneParams(idParams)
            );

            var itemStatuses = await Repo.ExecuteSqlSelectAsync<StatusValue>(
                itemStatusesQuery,
                CloneParams(idParams)
            );

            var dishIds = orderDetails.Select(d => d.DishId).Distinct().ToList();
            var dishes = new List<Models.Menu.Dish>();
            if (dishIds.Any())
            {
                dishes = (await Repo.GetAsync<Models.Menu.Dish>(d => dishIds.Contains(d.Id))).ToList();
            }
            var dishesById = dishes.ToDictionary(d => d.Id, d => d);

            var detailsByOrderId = orderDetails
                .GroupBy(d => d.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => GroupOrderDetailsByDish(g) 
                );

            var statusById = itemStatuses
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var paidPayments = await Repo.GetAsync<Payment>(
                p => p.OrderId.HasValue && orderIds.Contains(p.OrderId.Value) && p.Status == PaymentStatus.Success);
            var paidOrderIds = paidPayments.Select(p => p.OrderId!.Value).ToHashSet();

            var tableSessions = await Repo.GetAsync<Models.Reservations.TableSession>(
                filter: ts => ts.OrderId.HasValue && orderIds.Contains(ts.OrderId.Value),
                includeProperties: "Table"
            );

            var sessionsByOrderId = tableSessions
                .Where(ts => ts.OrderId.HasValue)
                .GroupBy(ts => ts.OrderId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var o in orders)
            {
                if (detailsByOrderId.TryGetValue(o.Id, out var ods))
                {
                    foreach (var d in ods)
                    {
                        if (statusById.TryGetValue(d.ItemStatusId, out var s))
                            d.ItemStatus = s;

                        if (dishesById.TryGetValue(d.DishId, out var dish))
                        {
                            d.Dish = dish;
                        }
                    }

                    o.OrderDetails = ods;
                }

                if (sessionsByOrderId.TryGetValue(o.Id, out var sessions))
                {
                    o.TableSessions = sessions;
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
                includeProperties: "OrderDetails,OrderDetails.Dish,OrderDetails.ItemStatus,Payments,Customer,Customer.ApplicationUser,Reservation,TableSessions,TableSessions.Table"
            ); 

            order.OrderDetails = GroupOrderDetailsByDish(order.OrderDetails);

            var mappedOrder = mapper.Map<DataTranferObjects.Orders.Order>(order);
            mappedOrder.Customer.TotalOrders = await Repo.ExecuteSqlCommandAsync<int>(
                            "SELECT COUNT(*) FROM Orders WHERE CustomerId = @CustomerId",
                            new SqlParameter("CustomerId", order.CustomerId));
            mappedOrder.Customer.TotalReservations = await Repo.ExecuteSqlCommandAsync<int>(
                "SELECT COUNT(*) FROM Reservations WHERE CustomerId = @CustomerId",
                new SqlParameter("CustomerId", order.CustomerId));

            return mappedOrder;
        }

        public async Task<DataTranferObjects.Orders.Order> CheckSessionBeforeOrder(DataTranferObjects.Orders.Order order, string userId)
        {
            var activeSession = await tableService.GetActiveTableSession(order.TableId);

            if (activeSession != null)
            {
                bool isOrderSession = activeSession.OrderId.HasValue;

                if (isOrderSession)
                {
                    order.Id = activeSession.OrderId;
                }
                else
                {
                    order.ReservationId = activeSession.ReservationId;
                }

                var updatedOrder = await UpsertOrder(order, userId);

                if (!isOrderSession)
                {
                    var reservationId = activeSession.ReservationId;
                    var reservationSessions = await Repo.GetAsync<Models.Reservations.TableSession>(
                            filter: ts => ts.ReservationId == reservationId && ts.IsActive
                        );

                    foreach (var session in reservationSessions)
                    {
                        session.OrderId = updatedOrder.Id;
                        Repo.Update(session, userId);
                    }

                    await Repo.SaveAsync();
                }

                return mapper.Map<DataTranferObjects.Orders.Order>(updatedOrder);
            }
            else
            {
                var newSession = await tableService.CreateTableSession(order.TableId, userId, order.CustomerId);
                newSession.Order = await UpsertOrder(order, userId);
                Repo.Update(newSession, userId);
                await Repo.SaveAsync();
                return mapper.Map<DataTranferObjects.Orders.Order>(newSession.Order);
            }

        }

        public async Task<Models.Orders.Order> UpsertOrder(DataTranferObjects.Orders.Order order, string userId)
        {
            Models.Orders.Order orderEntity;

            // CREATE
            if (order.Id == null)
            {
                orderEntity = new Models.Orders.Order
                {
                    Reference = await GetNextOrderReference(),
                    CustomerId = order.CustomerId,
                    ReservationId = order.ReservationId,
                    OrderDetails = new List<Models.Orders.OrderDetail>(),
                    SubTotal = 0
                };
                var dishIds = (order.OrderDetails ?? new List<DataTranferObjects.Orders.OrderDetail>())
                    .Select(x => x.DishId)
                    .Distinct()
                    .ToList();

                var dishes = (await Repo.GetAsync<Models.Menu.Dish>(d => dishIds.Contains(d.Id))).ToList();
                var dishesById = dishes.ToDictionary(d => d.Id, d => d);

                decimal additionalSubTotal = 0;

                var statuses = await statusValueService.GetStatuses("order-detail");
                int itemDefaultStatusId = statuses.First(x => x.IsDefault).Id;

                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    foreach (var d in order.OrderDetails)
                    {
                        if (!dishesById.ContainsKey(d.DishId))
                            throw new Exception($"Dish {d.DishId} not found");

                        var dish = dishesById[d.DishId];

                        additionalSubTotal += d.Quantity * dish.Price;

                        orderEntity.OrderDetails.Add(new Models.Orders.OrderDetail
                        {
                            DishId = d.DishId,
                            Quantity = d.Quantity,
                            Note = d.Note,
                            ItemStatusId = itemDefaultStatusId
                        });
                    }
                }

                orderEntity.SubTotal += additionalSubTotal;
                orderEntity.CalculateTotalAmount();

                await Repo.CreateAsync(orderEntity, userId);
            }
            else
            {
                orderEntity = await Repo.GetOneAsync<Models.Orders.Order>(
                    filter: o => o.Id == order.Id
                );

                if (orderEntity == null)
                    throw new Exception("Order not found");

                var dishIds = (order.OrderDetails ?? new List<DataTranferObjects.Orders.OrderDetail>())
                    .Select(x => x.DishId)
                    .Distinct()
                    .ToList();

                var dishes = (await Repo.GetAsync<Models.Menu.Dish>(d => dishIds.Contains(d.Id))).ToList();
                var dishesById = dishes.ToDictionary(d => d.Id, d => d);

                decimal additionalSubTotal = 0;

                var statuses = await statusValueService.GetStatuses("order-detail");
                int itemDefaultStatusId = statuses.First(x => x.IsDefault).Id;

                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    foreach (var d in order.OrderDetails)
                    {
                        if (!dishesById.ContainsKey(d.DishId))
                            throw new Exception($"Dish {d.DishId} not found");

                        var dish = dishesById[d.DishId];

                        var newDetail = new Models.Orders.OrderDetail
                        {
                            OrderId = orderEntity.Id,
                            DishId = d.DishId,
                            Quantity = d.Quantity,
                            Note = d.Note,
                            ItemStatusId = itemDefaultStatusId
                        };

                        additionalSubTotal += d.Quantity * dish.Price;

                        await Repo.CreateAsync(newDetail, userId);
                    }
                }

                orderEntity.SubTotal += additionalSubTotal;
                orderEntity.CalculateTotalAmount();

                Repo.Update(orderEntity, userId);
                await Repo.SaveAsync();
            }

            return orderEntity;
        }

        public async Task<Guid> UpdateOrder(Guid id, DataTranferObjects.Orders.Order order, string userId)
        {
            Models.Orders.Order? orderEntity = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == id,
                includeProperties: "OrderDetails,Payments"
            );

            if (orderEntity == null)
            {
                return Guid.Empty;
            }

            var dishIds = (order.OrderDetails ?? new List<DataTranferObjects.Orders.OrderDetail>())
                .Select(x => x.DishId)
                .Distinct()
                .ToList();

            var dishes = (await Repo.GetAsync<Dish>(filter: d => dishIds.Contains(d.Id))).ToList();
            var dishesById = dishes.ToDictionary(d => d.Id, d => d);

            decimal discountAmount = order.DiscountAmount ?? 0m;
            decimal taxAmount = order.TaxAmount ?? 0m;
            decimal serviceCharge = order.ServiceCharge ?? 0m;
            decimal subTotal = (order.OrderDetails?.Sum(x => x.Quantity * dishesById[x.DishId].Price)) ?? 0m;

            var statuses = await statusValueService.GetStatuses("order-detail");
            int itemDefaultStatusId = statuses.First(x => x.IsDefault).Id;

            orderEntity.Reference = order.Reference ?? orderEntity.Reference;
            orderEntity.CustomerId = order.CustomerId;
            orderEntity.ReservationId = order.ReservationId;
            orderEntity.DiscountAmount = discountAmount;
            orderEntity.TaxAmount = taxAmount;
            orderEntity.ServiceCharge = serviceCharge;
            orderEntity.CompletedAt = order.CompletedAt;
            orderEntity.CancelledAt = order.CancelledAt;
            orderEntity.HandledBy = order.HandledBy;

            if (orderEntity.OrderDetails?.Any() == true)
            {
                foreach (Models.Orders.OrderDetail d in orderEntity.OrderDetails.ToList())
                {
                    Repo.Delete<Models.Orders.OrderDetail>(d.Id);
                }
            }

            if (order.OrderDetails?.Any() == true)
            {
                foreach (DataTranferObjects.Orders.OrderDetail d in order.OrderDetails)
                {
                    Models.Orders.OrderDetail newDetail = new Models.Orders.OrderDetail
                    {
                        OrderId = orderEntity.Id,
                        DishId = d.DishId,
                        Quantity = d.Quantity,
                        Note = d.Note,
                        ItemStatusId = itemDefaultStatusId
                    };
                    orderEntity.OrderDetails.Add(newDetail);
                }
            }

            orderEntity.SubTotal = subTotal;
            orderEntity.CalculateTotalAmount();

            Repo.Update(orderEntity, userId);
            await Repo.SaveAsync();

            return orderEntity.Id;
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

            if (order == null) throw new AppException("No order");

            if (userId != String.Empty) order.HandledBy = Guid.Parse(userId);

            order.OrderStatusId = statusId;

            // Minus ingredient base on DishRecipe -> Trigger call method 
            //if (statusId == (int)OrderStatus.Cancelled)
            //    order.CancelledAt = DateTime.UtcNow.AddHours(7);
            //else if (statusId == (int)OrderStatus.Completed)
            //    order.CompletedAt = DateTime.UtcNow.AddHours(7);

            //if (oldStatus != OrderStatus.Confirmed && statusId == (int)OrderStatus.Confirmed)
            //{
            //    var orderDetails = await Repo.GetAsync<Models.Orders.OrderDetail>(
            //        filter: od => od.OrderId == orderId,
            //        includeProperties: "Dish"
            //    );

            //    foreach (var detail in orderDetails)
            //    {
            //        var hasRecipe = await Repo.GetExistsAsync<DishRecipe>(r => r.DishId == detail.DishId);
            //        if (hasRecipe)
            //        {
            //            await ingredientService.DeductFromRecipe(detail.DishId, detail.Quantity);
            //        }
            //    }
            //}

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

        public async Task<IEnumerable<DataTranferObjects.Orders.OrderDetail>> GetAllOrderDetails()
        {
            var orderDetailStatuses = await statusValueService.GetStatuses("order-detail");

            var initialStatusId = orderDetailStatuses.FirstOrDefault(x => x.IsDefault)?.Id
                                  ?? orderDetailStatuses.FirstOrDefault()?.Id;

            var orderDetails = await Repo.GetAsync<Models.Orders.OrderDetail>(
                filter: od => od.ItemStatusId == initialStatusId,
                orderBy: query => query.OrderBy(od => od.CreatedDate),
                includeProperties: "ItemStatus,Order,Dish"
            );

            return mapper.Map<IEnumerable<DataTranferObjects.Orders.OrderDetail>>(orderDetails);
        }

        private async Task<string> GetNextOrderReference()
        {
            string tenantPrefix = CurrentTenant.Prefix;
            string reference = $"{tenantPrefix}{DateTime.UtcNow.AddHours(7):yMdsff}";

            bool exists = await Repo.GetExistsAsync<Models.Orders.Order>(o => o.Reference == reference);
            int count = 0;

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

        private List<Models.Orders.OrderDetail> GroupOrderDetailsByDish(IEnumerable<Models.Orders.OrderDetail> orderDetails)
        {
            if (orderDetails == null || !orderDetails.Any())
            {
                return new List<Models.Orders.OrderDetail>();
            }

            return orderDetails
                .GroupBy(d => new { d.DishId, d.ItemStatusId })
                .Select(dishGroup =>
                {
                    var firstItem = dishGroup.First();

                    firstItem.Quantity = dishGroup.Sum(x => x.Quantity);

                    var notes = dishGroup
                        .Where(x => !string.IsNullOrWhiteSpace(x.Note))
                        .Select(x => x.Note.Trim())
                        .Distinct();
                    firstItem.Note = string.Join("; ", notes);

                    return firstItem;
                })
                .ToList();
        }


        public async Task<byte[]> ExportAsync(OrderSearch filter)
        {
            ExcelPackage.License.SetNonCommercialPersonal("RestX");
            filter.Page = 1;
            filter.ItemsPerPage = int.MaxValue;
            var result = await GetAllOrders(filter);
            var orders = result.Orders;

            if (!orders.Any())
                return ExcelHelper.CreateEmptyWorkbook("Orders");
            var statuses = await statusValueService.GetStatuses("order");
            var statusById = statuses.ToDictionary(s => s.Id, s => s.Name);

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Orders");
            var headers = new[]
            {
                "Reference", "Customer Name", "Customer Email", "Order Status", "Sub Total", "Discount",
                "Tax", "Service Charge", "Total Amount", "Payment Status",
                "Item Count", "Created Date", "Completed At", "Cancelled At"
            };
            ExcelHelper.WriteHeaders(sheet, headers);

            int row = 2;
            foreach (var o in orders)
            {
                sheet.Cells[row, 1].Value = o.Reference;
                sheet.Cells[row, 2].Value = o.Customer?.FullName ?? "";
                sheet.Cells[row, 3].Value = o.Customer?.Email ?? "";
                sheet.Cells[row, 4].Value = statusById.TryGetValue((int)o.OrderStatusId, out var statusName) ? statusName : o.OrderStatusId.ToString();
                sheet.Cells[row, 5].Value = o.SubTotal ?? 0;
                sheet.Cells[row, 6].Value = o.DiscountAmount ?? 0;
                sheet.Cells[row, 7].Value = o.TaxAmount ?? 0;
                sheet.Cells[row, 8].Value = o.ServiceCharge ?? 0;
                sheet.Cells[row, 9].Value = o.TotalAmount;
                sheet.Cells[row, 10].Value = o.PaymentStatusName;
                sheet.Cells[row, 11].Value = o.OrderDetails?.Sum(d => d.Quantity) ?? 0;
                sheet.Cells[row, 12].Value = o.CreatedDate.HasValue ? o.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm") : "";
                sheet.Cells[row, 13].Value = o.CompletedAt.HasValue ? o.CompletedAt.Value.ToString("dd/MM/yyyy HH:mm") : "";
                sheet.Cells[row, 14].Value = o.CancelledAt.HasValue ? o.CancelledAt.Value.ToString("dd/MM/yyyy HH:mm") : "";
                foreach (var col in new[] { 5, 6, 7, 8, 9 })
                    sheet.Cells[row, col].Style.Numberformat.Format = "#,##0";
                row++;
            }

            ExcelHelper.AutoFitAndStyle(sheet, headers.Length, row - 1);
            return package.GetAsByteArray();
        }
        public async Task<ApplyDiscountResponse> ApplyDiscount(Guid orderId, ApplyDiscountRequest request)
        {
            var order = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == orderId,
                includeProperties: "PromotionHistories")
                ?? throw new KeyNotFoundException("Order not found");

            var alreadyPaid = await Repo.GetExistsAsync<Payment>(
                p => p.OrderId == orderId && p.Status == PaymentStatus.Success);
            if (alreadyPaid)
                throw new InvalidOperationException("Cannot apply discount to a paid order");
            var response = new ApplyDiscountResponse
            {
                OrderId = orderId,
                SubTotal = order.SubTotal,
                TaxAmount = order.TaxAmount,
                ServiceCharge = order.ServiceCharge
            };

            decimal promotionDiscount = 0;
            decimal membershipDiscount = 0;
            if (!string.IsNullOrWhiteSpace(request.PromotionCode))
            {
                var now = DateTime.UtcNow.AddHours(7);
                var promotion = await Repo.GetOneAsync<Models.Promotions.Promotion>(
                    filter: p =>
                        p.Code == request.PromotionCode.Trim().ToUpperInvariant()
                        && p.IsActive
                        && p.ValidFrom <= now
                        && p.ValidTo >= now);

                if (promotion == null)
                {
                    response.PromotionError = "Mã giảm giá không hợp lệ hoặc đã hết hạn";
                }
                else if (promotion.MinOrderAmount > 0 && order.SubTotal < promotion.MinOrderAmount)
                {
                    response.PromotionError = $"Đơn hàng tối thiểu {promotion.MinOrderAmount:N0} VND để áp dụng mã này";
                }
                else
                {
                    if (promotion.UsageLimit > 0)
                    {
                        var totalUsage = await Repo.GetCountAsync<PromotionHistory>(
                            ph => ph.PromotionId == promotion.Id);
                        if (totalUsage >= promotion.UsageLimit)
                        {
                            response.PromotionError = "Mã giảm giá đã hết lượt sử dụng";
                        }
                    }

                    if (response.PromotionError == null && promotion.UsagePerCustomer > 0 && order.CustomerId.HasValue)
                    {
                        var customerOrders = await Repo.GetAsync<Models.Orders.Order>(
                            o => o.CustomerId == order.CustomerId);
                        var customerOrderIds = customerOrders.Select(o => o.Id).ToList();
                        var customerUsage = await Repo.GetCountAsync<PromotionHistory>(
                            ph => ph.PromotionId == promotion.Id && customerOrderIds.Contains(ph.OrderId));
                        if (customerUsage >= promotion.UsagePerCustomer)
                        {
                            response.PromotionError = "Bạn đã sử dụng hết lượt cho mã giảm giá này";
                        }
                    }

                    if (response.PromotionError == null)
                    {
                        // Load promotion applicable items + order details với dish để tính applicable subtotal
                        var applicableItems = await Repo.GetAsync<PromotionApplicableItem>(
                            pi => pi.PromotionId == promotion.Id);

                        var orderDetails = await Repo.GetAsync<Models.Orders.OrderDetail>(
                            od => od.OrderId == orderId,
                            includeProperties: "Dish");

                        decimal applicableSubTotal;
                        if (!applicableItems.Any())
                        {
                            // Không giới hạn item → áp toàn bộ subtotal
                            applicableSubTotal = order.SubTotal;
                        }
                        else
                        {
                            var applicableDishIds = applicableItems
                                .Where(ai => ai.DishId.HasValue)
                                .Select(ai => ai.DishId!.Value)
                                .ToHashSet();

                            var applicableCategoryIds = applicableItems
                                .Where(ai => ai.CategoryId.HasValue)
                                .Select(ai => ai.CategoryId!.Value)
                                .ToHashSet();

                            applicableSubTotal = orderDetails.Sum(od =>
                            {
                                if (od.Dish == null) return 0m;
                                bool matches = applicableDishIds.Contains(od.DishId)
                                    || applicableCategoryIds.Contains(od.Dish.CategoryId);
                                return matches ? od.Quantity * od.Dish.Price : 0m;
                            });
                        }

                        if (string.Equals(promotion.DiscountType, "PERCENTAGE", StringComparison.OrdinalIgnoreCase))
                        {
                            promotionDiscount = applicableSubTotal * promotion.DiscountValue / 100;
                            if (promotion.MaxDiscountAmount > 0 && promotionDiscount > promotion.MaxDiscountAmount)
                                promotionDiscount = promotion.MaxDiscountAmount;
                        }
                        else
                        {
                            promotionDiscount = Math.Min(promotion.DiscountValue, applicableSubTotal);
                        }

                        promotionDiscount = Math.Min(promotionDiscount, order.SubTotal);

                        response.AppliedPromotion = new AppliedPromotionInfo
                        {
                            Code = promotion.Code,
                            Name = promotion.Name,
                            DiscountType = promotion.DiscountType,
                            DiscountValue = promotion.DiscountValue,
                            MaxDiscountAmount = promotion.MaxDiscountAmount
                        };

                        var existingHistory = order.PromotionHistories
                            .FirstOrDefault(ph => ph.PromotionId == promotion.Id);
                        if (existingHistory != null)
                        {
                            existingHistory.DiscountAmount = promotionDiscount;
                            Repo.Update(existingHistory);
                        }
                        else
                        {
                            foreach (var old in order.PromotionHistories.ToList())
                            {
                                if (old.PromotionId != promotion.Id)
                                    Repo.Delete(old);
                            }

                            await Repo.CreateAsync(new PromotionHistory
                            {
                                PromotionId = promotion.Id,
                                OrderId = orderId,
                                DiscountAmount = promotionDiscount
                            });
                        }
                    }
                    else
                    {
                        foreach (var old in order.PromotionHistories.ToList())
                            Repo.Delete(old);
                    }
                }
            }
            else
            {
                foreach (var old in order.PromotionHistories.ToList())
                    Repo.Delete(old);
            }

            if (request.ApplyMembership && order.CustomerId.HasValue)
            {
                var customer = await Repo.GetOneAsync<Customer>(c => c.Id == order.CustomerId.Value);
                if (customer != null && !string.IsNullOrEmpty(customer.MembershipLevel))
                {
                    var band = await Repo.GetOneAsync<LoyaltyPointBand>(
                        b => b.IsActive && b.Name == customer.MembershipLevel);
                    if (band != null && band.DiscountPercentage > 0)
                    {
                        membershipDiscount = (order.SubTotal - promotionDiscount) * band.DiscountPercentage / 100;
                        membershipDiscount = Math.Max(0, membershipDiscount);

                        response.AppliedMembership = new AppliedMembershipInfo
                        {
                            Level = customer.MembershipLevel,
                            DiscountPercentage = band.DiscountPercentage
                        };
                    }
                }
            }

            var totalDiscount = Math.Min(promotionDiscount + membershipDiscount, order.SubTotal);
            response.Breakdown = new DiscountBreakdown
            {
                PromotionDiscount = promotionDiscount,
                MembershipDiscount = membershipDiscount
            };
            response.DiscountAmount = totalDiscount;
            order.DiscountAmount = totalDiscount;
            order.CalculateTotalAmount();
            response.TotalAmount = order.TotalAmount;

            Repo.Update(order);
            await Repo.SaveAsync();

            return response;
        }

        public async Task RemoveDiscount(Guid orderId)
        {
            var order = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == orderId,
                includeProperties: "PromotionHistories")
                ?? throw new KeyNotFoundException("Order not found");

            var alreadyPaid = await Repo.GetExistsAsync<Payment>(
                p => p.OrderId == orderId && p.Status == PaymentStatus.Success);
            if (alreadyPaid)
                throw new InvalidOperationException("Cannot modify a paid order");

            foreach (var old in order.PromotionHistories.ToList())
                Repo.Delete(old);
            order.DiscountAmount = 0;
            order.CalculateTotalAmount();

            Repo.Update(order);
            await Repo.SaveAsync();
        }

    }
}
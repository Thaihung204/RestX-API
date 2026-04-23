using AutoMapper;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using OfficeOpenXml;
using PayOS.Resources.V1.Payouts.Batch;
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
using RestX.Models.Enum;
using RestX.Models.Identity;
using RestX.Models.Loyalty;
using RestX.Models.Menu;
using RestX.Models.Orders;
using RestX.Models.Promotions;
using RestX.Models.Reservations;
using RestX.Models.Tables;
using RestX.Models.Tenants;
using StackExchange.Redis;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

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

        public async Task<OrderSearchResult> GetCurrentOrders(OrderSearch model)
        {
            if (model.Page <= 0) model.Page = 1;
            if (model.ItemsPerPage <= 0) model.ItemsPerPage = 20;
            if (model.ItemsPerPage > 200) model.ItemsPerPage = 200;

            OrderSearchResult result = new OrderSearchResult
            {
                Page = model.Page,
                ItemsPerPage = model.ItemsPerPage
            };

            StringBuilder query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM dbo.Orders o
                WHERE 1 = 1
            ");

            List<SqlParameter> countParams = new List<SqlParameter>();
            List<SqlParameter> queryParams = new List<SqlParameter>();

            DateTime now = DateTime.UtcNow.AddHours(7);

            query.Append(@"
                AND (
                    o.OrderStatusId <> @OpenStatus
                    OR EXISTS (
                        SELECT 1
                        FROM dbo.TableSessions ts
                        WHERE ts.OrderId = o.Id
                          AND ts.IsActive = 1
                          AND ts.StartedAt <= @Now
                    )
                )
            ");

            countParams.Add(new SqlParameter("OpenStatus", SqlDbType.Int) { Value = (int)OrderStatus.Open });
            queryParams.Add(new SqlParameter("OpenStatus", SqlDbType.Int) { Value = (int)OrderStatus.Open });

            countParams.Add(new SqlParameter("Now", SqlDbType.DateTime2) { Value = now });
            queryParams.Add(new SqlParameter("Now", SqlDbType.DateTime2) { Value = now });

            if (model.Status.HasValue)
            {
                int statusValue = (int)model.Status.Value;
                query.Append(" AND o.OrderStatusId = @Status ");

                countParams.Add(new SqlParameter("Status", SqlDbType.Int) { Value = statusValue });
                queryParams.Add(new SqlParameter("Status", SqlDbType.Int) { Value = statusValue });
            }

            if (!string.IsNullOrWhiteSpace(model.Reference))
            {
                string reference = model.Reference.Trim();
                query.Append(" AND o.Reference LIKE @Reference ");

                countParams.Add(new SqlParameter("Reference", SqlDbType.NVarChar) { Value = $"%{reference}%" });
                queryParams.Add(new SqlParameter("Reference", SqlDbType.NVarChar) { Value = $"%{reference}%" });
            }

            if (model.Total.HasValue)
            {
                query.Append(" AND o.TotalAmount = @Total ");

                countParams.Add(new SqlParameter("Total", SqlDbType.Decimal) { Value = model.Total.Value });
                queryParams.Add(new SqlParameter("Total", SqlDbType.Decimal) { Value = model.Total.Value });
            }

            if (model.ItemCount.HasValue)
            {
                query.Append(@"
                    AND (
                        SELECT ISNULL(SUM(od.Quantity), 0)
                        FROM dbo.OrderDetails od
                        WHERE od.OrderId = o.Id
                    ) = @ItemCount
                ");

                countParams.Add(new SqlParameter("ItemCount", SqlDbType.Int) { Value = model.ItemCount.Value });
                queryParams.Add(new SqlParameter("ItemCount", SqlDbType.Int) { Value = model.ItemCount.Value });
            }

            if (model.PaymentStatus.HasValue)
            {
                int paymentStatusValue = (int)model.PaymentStatus.Value;

                query.Append(@"
                    AND EXISTS (
                        SELECT 1
                        FROM dbo.Payments p
                        WHERE p.OrderId = o.Id
                          AND p.Status = @PaymentStatus
                    )
                ");

                countParams.Add(new SqlParameter("PaymentStatus", SqlDbType.Int) { Value = paymentStatusValue });
                queryParams.Add(new SqlParameter("PaymentStatus", SqlDbType.Int) { Value = paymentStatusValue });
            }

            if (!string.IsNullOrWhiteSpace(model.CustomerName))
            {
                string customerName = model.CustomerName.Trim();

                List<Models.Customers.Customer> matchedCustomers = (await Repo.GetAsync<Models.Customers.Customer>(
                    filter: c => c.ApplicationUser != null && c.ApplicationUser.FullName.Contains(customerName),
                    includeProperties: "ApplicationUser")).ToList();

                List<Guid> matchedCustomerIds = matchedCustomers.Select(c => c.Id).Distinct().ToList();

                if (!matchedCustomerIds.Any())
                {
                    query.Append(" AND 1 = 0 ");
                }
                else
                {
                    List<string> customerParamNames = new List<string>();
                    for (int i = 0; i < matchedCustomerIds.Count; i++)
                    {
                        string paramName = $"CustomerId{i}";
                        customerParamNames.Add("@" + paramName);

                        countParams.Add(new SqlParameter(paramName, SqlDbType.UniqueIdentifier) { Value = matchedCustomerIds[i] });
                        queryParams.Add(new SqlParameter(paramName, SqlDbType.UniqueIdentifier) { Value = matchedCustomerIds[i] });
                    }

                    query.Append($" AND o.CustomerId IN ({string.Join(", ", customerParamNames)}) ");
                }
            }

            if (model.Time.HasValue)
            {
                DateTime timeFromUtc = model.Time.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(model.Time.Value, DateTimeKind.Utc)
                    : model.Time.Value.ToUniversalTime();

                DateTime timeToUtcExclusive = timeFromUtc.Date.AddDays(1);

                query.Append(" AND o.CreatedDate >= @TimeFrom AND o.CreatedDate < @TimeToExclusive ");

                countParams.Add(new SqlParameter("TimeFrom", SqlDbType.DateTime2) { Value = timeFromUtc.Date });
                countParams.Add(new SqlParameter("TimeToExclusive", SqlDbType.DateTime2) { Value = timeToUtcExclusive });

                queryParams.Add(new SqlParameter("TimeFrom", SqlDbType.DateTime2) { Value = timeFromUtc.Date });
                queryParams.Add(new SqlParameter("TimeToExclusive", SqlDbType.DateTime2) { Value = timeToUtcExclusive });
            }

            if (model.From.HasValue)
            {
                DateTime fromUtc = model.From.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(model.From.Value, DateTimeKind.Utc)
                    : model.From.Value.ToUniversalTime();

                query.Append(" AND o.CreatedDate >= @From ");

                countParams.Add(new SqlParameter("From", SqlDbType.DateTime2) { Value = fromUtc });
                queryParams.Add(new SqlParameter("From", SqlDbType.DateTime2) { Value = fromUtc });
            }

            if (model.To.HasValue)
            {
                DateTime toUtcExclusive = (model.To.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(model.To.Value, DateTimeKind.Utc)
                        : model.To.Value.ToUniversalTime())
                    .Date
                    .AddDays(1);

                query.Append(" AND o.CreatedDate < @ToExclusive ");

                countParams.Add(new SqlParameter("ToExclusive", SqlDbType.DateTime2) { Value = toUtcExclusive });
                queryParams.Add(new SqlParameter("ToExclusive", SqlDbType.DateTime2) { Value = toUtcExclusive });
            }

            string countQuery = query.ToString().Replace("#SELECT#", "COUNT(1)");
            result.TotalCount = await Repo.ExecuteSqlCommandAsync<int>(
                countQuery,
                countParams.Any() ? countParams.Cast<object>().ToArray() : null
            );

            result.TotalPages = result.ItemsPerPage <= 0
                ? 0
                : (int)Math.Ceiling((decimal)result.TotalCount / result.ItemsPerPage);

            int skip = result.Page <= 1 ? 0 : (result.Page - 1) * result.ItemsPerPage;

            string selectItems = @"
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
                o.CreatedDate,
                o.ModifiedDate,
                o.CreatedBy,
                o.ModifiedBy
            ";

            string mainQuery = query.ToString().Replace("#SELECT#", selectItems);

            mainQuery += (model.SortBy?.ToLowerInvariant()) switch
            {
                "created_asc" => " ORDER BY o.CreatedDate ASC",
                _ => " ORDER BY o.CreatedDate DESC"
            };

            mainQuery += $" OFFSET {skip} ROWS FETCH NEXT {result.ItemsPerPage} ROWS ONLY";

            List<Models.Orders.Order> orders = await Repo.ExecuteSqlSelectAsync<Models.Orders.Order>(
                mainQuery,
                queryParams.Any() ? queryParams.Cast<object>().ToArray() : null
            );

            if (orders.Count == 0)
            {
                result.Orders = new List<DataTranferObjects.Orders.Order>();
                return result;
            }

            List<Guid> orderIds = orders.Select(o => o.Id).ToList();
            List<SqlParameter> idParams = orderIds
                .Select((id, i) => new SqlParameter($"OrderId{i}", SqlDbType.UniqueIdentifier) { Value = id })
                .ToList();

            string inClause = string.Join(", ", idParams.Select(p => "@" + p.ParameterName));

            string orderDetailsQuery = $@"
                SELECT
                    od.Id,
                    od.OrderId,
                    od.DishId,
                    od.Quantity,
                    od.UnitPrice,
                    od.Note,
                    od.ItemStatusId
                FROM dbo.OrderDetails od
                WHERE od.OrderId IN ({inClause})
                ORDER BY od.OrderId, od.Id
            ";

            string itemStatusesQuery = $@"
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

            List<Models.Orders.OrderDetail> orderDetails = await Repo.ExecuteSqlSelectAsync<Models.Orders.OrderDetail>(
                orderDetailsQuery,
                CloneParams(idParams)
            );

            List<StatusValue> itemStatuses = await Repo.ExecuteSqlSelectAsync<StatusValue>(
                itemStatusesQuery,
                CloneParams(idParams)
            );

            List<Guid> dishIds = orderDetails.Select(d => d.DishId).Distinct().ToList();
            List<Models.Menu.Dish> dishes = new List<Models.Menu.Dish>();
            if (dishIds.Any())
            {
                dishes = (await Repo.GetAsync<Models.Menu.Dish>(d => dishIds.Contains(d.Id))).ToList();
            }

            Dictionary<Guid, Models.Menu.Dish> dishesById = dishes.ToDictionary(d => d.Id, d => d);

            Dictionary<Guid, List<Models.Orders.OrderDetail>> detailsByOrderId = orderDetails
                .GroupBy(d => d.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<int, StatusValue> statusById = itemStatuses
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First());

            IEnumerable<Payment> paidPayments = await Repo.GetAsync<Payment>(
                p => p.OrderId.HasValue && orderIds.Contains(p.OrderId.Value) && p.Status == PaymentStatus.Success);
            HashSet<Guid> paidOrderIds = paidPayments.Select(p => p.OrderId!.Value).ToHashSet();

            IEnumerable<Models.Reservations.TableSession> tableSessions = await Repo.GetAsync<Models.Reservations.TableSession>(
                filter: ts => ts.OrderId.HasValue && orderIds.Contains(ts.OrderId.Value),
                includeProperties: "Table"
            );

            Dictionary<Guid, List<Models.Reservations.TableSession>> sessionsByOrderId = tableSessions
                .Where(ts => ts.OrderId.HasValue)
                .GroupBy(ts => ts.OrderId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (Models.Orders.Order o in orders)
            {
                if (detailsByOrderId.TryGetValue(o.Id, out List<Models.Orders.OrderDetail>? ods))
                {
                    foreach (Models.Orders.OrderDetail d in ods)
                    {
                        if (statusById.TryGetValue(d.ItemStatusId, out StatusValue? s))
                        {
                            d.ItemStatus = s;
                        }

                        if (dishesById.TryGetValue(d.DishId, out Models.Menu.Dish? dish))
                        {
                            d.Dish = dish;
                        }
                    }

                    o.OrderDetails = ods;
                }

                if (sessionsByOrderId.TryGetValue(o.Id, out List<Models.Reservations.TableSession>? sessions))
                {
                    o.TableSessions = sessions;
                }

                o.IsPaid = paidOrderIds.Contains(o.Id);
            }

            result.Orders = mapper.Map<List<DataTranferObjects.Orders.Order>>(orders);

            List<Guid?> customerIds = orders.Where(o => o.CustomerId != null).Select(o => o.CustomerId).Distinct().ToList();
            if (customerIds.Any())
            {
                IEnumerable<Models.Customers.Customer> customers = await Repo.GetAsync<Models.Customers.Customer>(
                    filter: c => customerIds.Contains(c.Id),
                    includeProperties: "ApplicationUser"
                );

                Dictionary<Guid, Models.Customers.Customer> customersDict = customers.ToDictionary(c => c.Id, c => c);

                foreach (DataTranferObjects.Orders.Order dtoOrder in result.Orders)
                {
                    if (customersDict.TryGetValue(dtoOrder.CustomerId, out Models.Customers.Customer? customer))
                    {
                        dtoOrder.CustomerName = customer.ApplicationUser?.FullName;
                        dtoOrder.CustomerEmail = customer.ApplicationUser?.Email;
                    }
                }
            }

            return result;
        }


        public async Task<OrderSearchResult> GetAllOrders(OrderSearch model)
        {
            if (model.Page <= 0) model.Page = 1;
            if (model.ItemsPerPage <= 0) model.ItemsPerPage = 20;
            if (model.ItemsPerPage > 200) model.ItemsPerPage = 200;

            OrderSearchResult result = new OrderSearchResult
            {
                Page = model.Page,
                ItemsPerPage = model.ItemsPerPage
            };

            StringBuilder query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM dbo.Orders o
                WHERE 1 = 1
            ");

            List<SqlParameter> countParams = new List<SqlParameter>();
            List<SqlParameter> queryParams = new List<SqlParameter>();

            //DateTime now = DateTime.UtcNow.AddHours(7);

            //query.Append(@"
            //    AND (
            //        o.OrderStatusId <> @OpenStatus
            //        OR EXISTS (
            //            SELECT 1
            //            FROM dbo.TableSessions ts
            //            WHERE ts.OrderId = o.Id
            //              AND ts.IsActive = 1
            //              AND ts.StartedAt <= @Now
            //        )
            //    )
            //");

            //countParams.Add(new SqlParameter("OpenStatus", SqlDbType.Int) { Value = (int)OrderStatus.Open });
            //queryParams.Add(new SqlParameter("OpenStatus", SqlDbType.Int) { Value = (int)OrderStatus.Open });

            //countParams.Add(new SqlParameter("Now", SqlDbType.DateTime2) { Value = now });
            //queryParams.Add(new SqlParameter("Now", SqlDbType.DateTime2) { Value = now });

            if (model.Status.HasValue)
            {
                int statusValue = (int)model.Status.Value;
                query.Append(" AND o.OrderStatusId = @Status ");

                countParams.Add(new SqlParameter("Status", SqlDbType.Int) { Value = statusValue });
                queryParams.Add(new SqlParameter("Status", SqlDbType.Int) { Value = statusValue });
            }

            if (!string.IsNullOrWhiteSpace(model.Reference))
            {
                string reference = model.Reference.Trim();
                query.Append(" AND o.Reference LIKE @Reference ");

                countParams.Add(new SqlParameter("Reference", SqlDbType.NVarChar) { Value = $"%{reference}%" });
                queryParams.Add(new SqlParameter("Reference", SqlDbType.NVarChar) { Value = $"%{reference}%" });
            }

            if (model.Total.HasValue)
            {
                query.Append(" AND o.TotalAmount = @Total ");

                countParams.Add(new SqlParameter("Total", SqlDbType.Decimal) { Value = model.Total.Value });
                queryParams.Add(new SqlParameter("Total", SqlDbType.Decimal) { Value = model.Total.Value });
            }

            if (model.ItemCount.HasValue)
            {
                query.Append(@"
                    AND (
                        SELECT ISNULL(SUM(od.Quantity), 0)
                        FROM dbo.OrderDetails od
                        WHERE od.OrderId = o.Id
                    ) = @ItemCount
                ");

                countParams.Add(new SqlParameter("ItemCount", SqlDbType.Int) { Value = model.ItemCount.Value });
                queryParams.Add(new SqlParameter("ItemCount", SqlDbType.Int) { Value = model.ItemCount.Value });
            }

            if (model.PaymentStatus.HasValue)
            {
                int paymentStatusValue = (int)model.PaymentStatus.Value;

                query.Append(@"
                    AND EXISTS (
                        SELECT 1
                        FROM dbo.Payments p
                        WHERE p.OrderId = o.Id
                          AND p.Status = @PaymentStatus
                    )
                ");

                countParams.Add(new SqlParameter("PaymentStatus", SqlDbType.Int) { Value = paymentStatusValue });
                queryParams.Add(new SqlParameter("PaymentStatus", SqlDbType.Int) { Value = paymentStatusValue });
            }

            if (!string.IsNullOrWhiteSpace(model.CustomerName))
            {
                string customerName = model.CustomerName.Trim();

                List<Models.Customers.Customer> matchedCustomers = (await Repo.GetAsync<Models.Customers.Customer>(
                    filter: c => c.ApplicationUser != null && c.ApplicationUser.FullName.Contains(customerName),
                    includeProperties: "ApplicationUser")).ToList();

                List<Guid> matchedCustomerIds = matchedCustomers.Select(c => c.Id).Distinct().ToList();

                if (!matchedCustomerIds.Any())
                {
                    query.Append(" AND 1 = 0 ");
                }
                else
                {
                    List<string> customerParamNames = new List<string>();
                    for (int i = 0; i < matchedCustomerIds.Count; i++)
                    {
                        string paramName = $"CustomerId{i}";
                        customerParamNames.Add("@" + paramName);

                        countParams.Add(new SqlParameter(paramName, SqlDbType.UniqueIdentifier) { Value = matchedCustomerIds[i] });
                        queryParams.Add(new SqlParameter(paramName, SqlDbType.UniqueIdentifier) { Value = matchedCustomerIds[i] });
                    }

                    query.Append($" AND o.CustomerId IN ({string.Join(", ", customerParamNames)}) ");
                }
            }

            if (model.Time.HasValue)
            {
                DateTime timeFromUtc = model.Time.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(model.Time.Value, DateTimeKind.Utc)
                    : model.Time.Value.ToUniversalTime();

                DateTime timeToUtcExclusive = timeFromUtc.Date.AddDays(1);

                query.Append(" AND o.CreatedDate >= @TimeFrom AND o.CreatedDate < @TimeToExclusive ");

                countParams.Add(new SqlParameter("TimeFrom", SqlDbType.DateTime2) { Value = timeFromUtc.Date });
                countParams.Add(new SqlParameter("TimeToExclusive", SqlDbType.DateTime2) { Value = timeToUtcExclusive });

                queryParams.Add(new SqlParameter("TimeFrom", SqlDbType.DateTime2) { Value = timeFromUtc.Date });
                queryParams.Add(new SqlParameter("TimeToExclusive", SqlDbType.DateTime2) { Value = timeToUtcExclusive });
            }

            if (model.From.HasValue)
            {
                DateTime fromUtc = model.From.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(model.From.Value, DateTimeKind.Utc)
                    : model.From.Value.ToUniversalTime();

                query.Append(" AND o.CreatedDate >= @From ");

                countParams.Add(new SqlParameter("From", SqlDbType.DateTime2) { Value = fromUtc });
                queryParams.Add(new SqlParameter("From", SqlDbType.DateTime2) { Value = fromUtc });
            }

            if (model.To.HasValue)
            {
                DateTime toUtcExclusive = (model.To.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(model.To.Value, DateTimeKind.Utc)
                        : model.To.Value.ToUniversalTime())
                    .Date
                    .AddDays(1);

                query.Append(" AND o.CreatedDate < @ToExclusive ");

                countParams.Add(new SqlParameter("ToExclusive", SqlDbType.DateTime2) { Value = toUtcExclusive });
                queryParams.Add(new SqlParameter("ToExclusive", SqlDbType.DateTime2) { Value = toUtcExclusive });
            }

            string countQuery = query.ToString().Replace("#SELECT#", "COUNT(1)");
            result.TotalCount = await Repo.ExecuteSqlCommandAsync<int>(
                countQuery,
                countParams.Any() ? countParams.Cast<object>().ToArray() : null
            );

            result.TotalPages = result.ItemsPerPage <= 0
                ? 0
                : (int)Math.Ceiling((decimal)result.TotalCount / result.ItemsPerPage);

            int skip = result.Page <= 1 ? 0 : (result.Page - 1) * result.ItemsPerPage;

            string selectItems = @"
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
                o.CreatedDate,
                o.ModifiedDate,
                o.CreatedBy,
                o.ModifiedBy
            ";

            string mainQuery = query.ToString().Replace("#SELECT#", selectItems);

            mainQuery += (model.SortBy?.ToLowerInvariant()) switch
            {
                "created_asc" => " ORDER BY o.CreatedDate ASC",
                _ => " ORDER BY o.CreatedDate DESC"
            };

            mainQuery += $" OFFSET {skip} ROWS FETCH NEXT {result.ItemsPerPage} ROWS ONLY";

            List<Models.Orders.Order> orders = await Repo.ExecuteSqlSelectAsync<Models.Orders.Order>(
                mainQuery,
                queryParams.Any() ? queryParams.Cast<object>().ToArray() : null
            );

            if (orders.Count == 0)
            {
                result.Orders = new List<DataTranferObjects.Orders.Order>();
                return result;
            }

            List<Guid> orderIds = orders.Select(o => o.Id).ToList();
            List<SqlParameter> idParams = orderIds
                .Select((id, i) => new SqlParameter($"OrderId{i}", SqlDbType.UniqueIdentifier) { Value = id })
                .ToList();

            string inClause = string.Join(", ", idParams.Select(p => "@" + p.ParameterName));

            string orderDetailsQuery = $@"
                SELECT
                    od.Id,
                    od.OrderId,
                    od.DishId,
                    od.Quantity,
                    od.UnitPrice,
                    od.Note,
                    od.ItemStatusId
                FROM dbo.OrderDetails od
                WHERE od.OrderId IN ({inClause})
                ORDER BY od.OrderId, od.Id
            ";

            string itemStatusesQuery = $@"
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

            List<Models.Orders.OrderDetail> orderDetails = await Repo.ExecuteSqlSelectAsync<Models.Orders.OrderDetail>(
                orderDetailsQuery,
                CloneParams(idParams)
            );

            List<StatusValue> itemStatuses = await Repo.ExecuteSqlSelectAsync<StatusValue>(
                itemStatusesQuery,
                CloneParams(idParams)
            );

            List<Guid> dishIds = orderDetails.Select(d => d.DishId).Distinct().ToList();
            List<Models.Menu.Dish> dishes = new List<Models.Menu.Dish>();
            if (dishIds.Any())
            {
                dishes = (await Repo.GetAsync<Models.Menu.Dish>(d => dishIds.Contains(d.Id))).ToList();
            }

            Dictionary<Guid, Models.Menu.Dish> dishesById = dishes.ToDictionary(d => d.Id, d => d);

            Dictionary<Guid, List<Models.Orders.OrderDetail>> detailsByOrderId = orderDetails
                .GroupBy(d => d.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<int, StatusValue> statusById = itemStatuses
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First());

            IEnumerable<Payment> paidPayments = await Repo.GetAsync<Payment>(
                p => p.OrderId.HasValue && orderIds.Contains(p.OrderId.Value) && p.Status == PaymentStatus.Success);
            HashSet<Guid> paidOrderIds = paidPayments.Select(p => p.OrderId!.Value).ToHashSet();

            IEnumerable<Models.Reservations.TableSession> tableSessions = await Repo.GetAsync<Models.Reservations.TableSession>(
                filter: ts => ts.OrderId.HasValue && orderIds.Contains(ts.OrderId.Value),
                includeProperties: "Table"
            );

            Dictionary<Guid, List<Models.Reservations.TableSession>> sessionsByOrderId = tableSessions
                .Where(ts => ts.OrderId.HasValue)
                .GroupBy(ts => ts.OrderId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (Models.Orders.Order o in orders)
            {
                if (detailsByOrderId.TryGetValue(o.Id, out List<Models.Orders.OrderDetail>? ods))
                {
                    foreach (Models.Orders.OrderDetail d in ods)
                    {
                        if (statusById.TryGetValue(d.ItemStatusId, out StatusValue? s))
                        {
                            d.ItemStatus = s;
                        }

                        if (dishesById.TryGetValue(d.DishId, out Models.Menu.Dish? dish))
                        {
                            d.Dish = dish;
                        }
                    }

                    o.OrderDetails = ods;
                }

                if (sessionsByOrderId.TryGetValue(o.Id, out List<Models.Reservations.TableSession>? sessions))
                {
                    o.TableSessions = sessions;
                }

                o.IsPaid = paidOrderIds.Contains(o.Id);
            }

            result.Orders = mapper.Map<List<DataTranferObjects.Orders.Order>>(orders);

            List<Guid?> customerIds = orders.Where(o => o.CustomerId != null).Select(o => o.CustomerId).Distinct().ToList();
            if (customerIds.Any())
            {
                IEnumerable<Models.Customers.Customer> customers = await Repo.GetAsync<Models.Customers.Customer>(
                    filter: c => customerIds.Contains(c.Id),
                    includeProperties: "ApplicationUser"
                );

                Dictionary<Guid, Models.Customers.Customer> customersDict = customers.ToDictionary(c => c.Id, c => c);

                foreach (DataTranferObjects.Orders.Order dtoOrder in result.Orders)
                {
                    if (customersDict.TryGetValue(dtoOrder.CustomerId, out Models.Customers.Customer? customer))
                    {
                        dtoOrder.CustomerName = customer.ApplicationUser?.FullName;
                        dtoOrder.CustomerEmail = customer.ApplicationUser?.Email;
                    }
                }
            }

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

            //order.OrderDetails = GroupOrderDetailsByDish(order.OrderDetails);

            var mappedOrder = mapper.Map<DataTranferObjects.Orders.Order>(order);
            if (order.Payments != null && order.Payments.Any())
            {
                if (order.Payments.Any(p => p.Status == Models.Enum.PaymentStatus.Success))
                    mappedOrder.PaymentStatus = Models.Enum.PaymentStatus.Success;
                else if (order.Payments.Any(p => p.Status == Models.Enum.PaymentStatus.Pending))
                    mappedOrder.PaymentStatus = Models.Enum.PaymentStatus.Pending;
                else
                    mappedOrder.PaymentStatus = Models.Enum.PaymentStatus.Fail;
            }

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
            var activeSession = await Repo.GetOneAsync<TableSession>(
                filter: ts => ts.TableId == order.TableId && ts.IsActive
                           && ts.StartedAt <= DateTime.UtcNow.AddHours(7)
            );

            //var activeSession = await tableService.GetActiveTableSession(order.TableId);

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

        private async Task ValidateOrderInput(DataTranferObjects.Orders.Order order)
        {
            if (order == null)
            {
                throw new AppException("Order data is required.");
            }

            if (order.CustomerId == Guid.Empty)
            {
                throw new AppException("CustomerId is required.");
            }

            bool customerExists = await Repo.GetExistsAsync<Customer>(c => c.Id == order.CustomerId);
            if (!customerExists)
            {
                throw new AppException("Customer not found.");
            }

            decimal discountAmount = order.DiscountAmount ?? 0m;
            decimal taxAmount = order.TaxAmount ?? 0m;
            decimal serviceCharge = order.ServiceCharge ?? 0m;

            if (discountAmount < 0 || taxAmount < 0 || serviceCharge < 0)
            {
                throw new AppException("DiscountAmount, TaxAmount, ServiceCharge cannot be negative.");
            }

            List<DataTranferObjects.Orders.OrderDetail> details = order.OrderDetails ?? new List<DataTranferObjects.Orders.OrderDetail>();
            if (!details.Any())
            {
                throw new AppException("Order must contain at least one order detail.");
            }

            foreach (DataTranferObjects.Orders.OrderDetail detail in details)
            {
                if (detail.DishId == Guid.Empty)
                {
                    throw new AppException("DishId is required.");
                }

                if (detail.Quantity <= 0)
                {
                    throw new AppException("Quantity must be greater than 0.");
                }

                if (!string.IsNullOrWhiteSpace(detail.Note) && detail.Note.Trim().Length > 500)
                {
                    throw new AppException("Order detail note cannot exceed 500 characters.");
                }
            }
        }

        public async Task<Models.Orders.Order> UpsertOrder(DataTranferObjects.Orders.Order order, string userId)
        {
            await ValidateOrderInput(order);
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

                int itemPreparingStatusId = await GetPreparingOrderDetailStatusId();

                Dictionary<Guid, int> dishQuantitiesToDeduct = new Dictionary<Guid, int>();

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
                            UnitPrice = dish.Price,
                            Note = d.Note,
                            ItemStatusId = itemPreparingStatusId
                        });

                        if (dishQuantitiesToDeduct.ContainsKey(d.DishId))
                        {
                            dishQuantitiesToDeduct[d.DishId] += d.Quantity;
                        }
                        else
                        {
                            dishQuantitiesToDeduct[d.DishId] = d.Quantity;
                        }
                    }
                }

                if (dishQuantitiesToDeduct.Any())
                {
                    await ingredientService.DeductFromRecipes(dishQuantitiesToDeduct);
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

                int itemPreparingStatusId = await GetPreparingOrderDetailStatusId();

                Dictionary<Guid, int> dishQuantitiesToDeduct = new Dictionary<Guid, int>();

                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    foreach (DataTranferObjects.Orders.OrderDetail d in order.OrderDetails)
                    {
                        if (!dishesById.ContainsKey(d.DishId))
                            throw new AppException("Dish is not exist");

                        Dish dish = dishesById[d.DishId];

                        Models.Orders.OrderDetail newDetail = new Models.Orders.OrderDetail
                        {
                            OrderId = orderEntity.Id,
                            DishId = d.DishId,
                            Quantity = d.Quantity,
                            UnitPrice = dish.Price,
                            Note = d.Note,
                            ItemStatusId = itemPreparingStatusId
                        };

                        additionalSubTotal += d.Quantity * dish.Price;

                        await Repo.CreateAsync(newDetail, userId);

                        if (dishQuantitiesToDeduct.ContainsKey(d.DishId))
                        {
                            dishQuantitiesToDeduct[d.DishId] += d.Quantity;
                        }
                        else
                        {
                            dishQuantitiesToDeduct[d.DishId] = d.Quantity;
                        }
                    }
                }

                if (dishQuantitiesToDeduct.Any())
                {
                    await ingredientService.DeductFromRecipes(dishQuantitiesToDeduct);
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
            await ValidateOrderInput(order);

            Models.Orders.Order? orderEntity = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == id,
                includeProperties: "OrderDetails,Payments"
            );

            if (orderEntity == null)
            {
                return Guid.Empty;
            }

            List<Guid> requestDishIds = (order.OrderDetails ?? new List<DataTranferObjects.Orders.OrderDetail>())
                .Select(x => x.DishId)
                .Distinct()
                .ToList();

            List<Dish> requestDishes = (await Repo.GetAsync<Dish>(filter: d => requestDishIds.Contains(d.Id))).ToList();
            Dictionary<Guid, Dish> requestDishesById = requestDishes.ToDictionary(d => d.Id, d => d);

            int itemPreparingStatusId = await GetPreparingOrderDetailStatusId();

            decimal discountAmount = order.DiscountAmount ?? 0m;
            decimal taxAmount = order.TaxAmount ?? 0m;
            decimal serviceCharge = order.ServiceCharge ?? 0m;

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
                List<Models.Orders.OrderDetail> oldPreparingDetails = orderEntity.OrderDetails
                    .Where(x => x.ItemStatusId == itemPreparingStatusId)
                    .ToList();

                if (oldPreparingDetails.Any())
                {
                    Dictionary<Guid, int> oldQtyByDishId = oldPreparingDetails
                        .GroupBy(x => x.DishId)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                    List<Guid> oldDishIds = oldQtyByDishId.Keys.ToList();

                    List<DishRecipe> oldRecipes = (await Repo.GetAsync<DishRecipe>(
                        filter: r => oldDishIds.Contains(r.DishId),
                        includeProperties: "Ingredient,Ingredient.InventoryStock"
                    )).ToList();

                    foreach (DishRecipe recipe in oldRecipes)
                    {
                        if (!oldQtyByDishId.TryGetValue(recipe.DishId, out int totalQty)) continue;

                        Models.Inventory.Ingredient? ingredient = recipe.Ingredient;
                        if (ingredient?.InventoryStock == null) continue;

                        decimal restoreQty = recipe.Quantity * totalQty;
                        ingredient.InventoryStock.CurrentQuantity += restoreQty;
                        ingredient.InventoryStock.LastUpdated = DateTime.UtcNow.AddHours(7);

                        ingredient.Status = ingredient.InventoryStock.CurrentQuantity == 0
                            ? IngredientStatus.OutOfStock
                            : ingredient.MinStockLevel > 0 && ingredient.InventoryStock.CurrentQuantity <= ingredient.MinStockLevel
                                ? IngredientStatus.LowStock
                                : IngredientStatus.InStock;
                    }
                }

                foreach (Models.Orders.OrderDetail d in orderEntity.OrderDetails.ToList())
                {
                    Repo.Delete<Models.Orders.OrderDetail>(d.Id);
                }
            }

            decimal subTotal = 0m;

            if (order.OrderDetails?.Any() == true)
            {
                foreach (DataTranferObjects.Orders.OrderDetail d in order.OrderDetails)
                {
                    if (!requestDishesById.ContainsKey(d.DishId))
                        throw new Exception($"Dish {d.DishId} not found");

                    Dish dish = requestDishesById[d.DishId];
                    subTotal += d.Quantity * dish.Price;

                    Models.Orders.OrderDetail newDetail = new Models.Orders.OrderDetail
                    {
                        OrderId = orderEntity.Id,
                        DishId = d.DishId,
                        Quantity = d.Quantity,
                        UnitPrice = dish.Price,
                        Note = d.Note,
                        ItemStatusId = itemPreparingStatusId
                    };

                    orderEntity.OrderDetails.Add(newDetail);

                    await Repo.CreateAsync(newDetail, userId);
                }
            }

            if (order.OrderDetails?.Any() == true)
            {
                Dictionary<Guid, int> newQtyByDishId = order.OrderDetails
                    .GroupBy(x => x.DishId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                await ingredientService.DeductFromRecipes(newQtyByDishId);
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
            if (!Enum.IsDefined(typeof(OrderStatus), statusId))
            {
                throw new AppException("Invalid order status.");
            }

            Models.Orders.Order? order = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == orderId,
                includeProperties: "OrderDetails"
            );

            if (order == null) throw new AppException("No order");

            int previousStatusId = order.OrderStatusId;
            order.OrderStatusId = statusId;

            if (previousStatusId == (int)OrderStatus.Open && statusId == (int)OrderStatus.Completed)
            {
                List<Models.Reservations.TableSession> activeSessions = (await Repo.GetAsync<Models.Reservations.TableSession>(
                    filter: ts => ts.OrderId == orderId && ts.IsActive
                )).ToList();

                foreach (Models.Reservations.TableSession session in activeSessions)
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow.AddHours(7);
                    Repo.Update(session, userId);
                }
            }

            if (statusId == (int)OrderStatus.Cancelled)
            {
                List<RestX.BLL.DataTranferObjects.Status.StatusValues> orderDetailStatuses =
                    (await statusValueService.GetStatuses("order-detail")).ToList();

                RestX.BLL.DataTranferObjects.Status.StatusValues? preparingStatus = orderDetailStatuses.FirstOrDefault(x =>
                    string.Equals(x.Code, "PREPARING", StringComparison.OrdinalIgnoreCase));
                RestX.BLL.DataTranferObjects.Status.StatusValues? cancelledStatus = orderDetailStatuses.FirstOrDefault(x =>
                    string.Equals(x.Code, "CANCELLED", StringComparison.OrdinalIgnoreCase));

                if (preparingStatus != null && cancelledStatus != null && order.OrderDetails?.Any() == true)
                {
                    List<Models.Orders.OrderDetail> preparingOrderDetails = order.OrderDetails
                        .Where(od => od.ItemStatusId == preparingStatus.Id)
                        .ToList();

                    if (preparingOrderDetails.Any())
                    {
                        Dictionary<Guid, int> qtyByDishId = preparingOrderDetails
                            .GroupBy(x => x.DishId)
                            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                        List<Guid> dishIds = qtyByDishId.Keys.ToList();

                        List<DishRecipe> recipes = (await Repo.GetAsync<DishRecipe>(
                            filter: r => dishIds.Contains(r.DishId),
                            includeProperties: "Ingredient,Ingredient.InventoryStock"
                        )).ToList();

                        foreach (DishRecipe recipe in recipes)
                        {
                            if (!qtyByDishId.TryGetValue(recipe.DishId, out int totalQty))
                            {
                                continue;
                            }

                            Models.Inventory.Ingredient? ingredient = recipe.Ingredient;
                            if (ingredient?.InventoryStock == null)
                            {
                                continue;
                            }

                            decimal restoreQty = recipe.Quantity * totalQty;
                            ingredient.InventoryStock.CurrentQuantity += restoreQty;
                            ingredient.InventoryStock.LastUpdated = DateTime.UtcNow.AddHours(7);

                            ingredient.Status = ingredient.InventoryStock.CurrentQuantity == 0
                                ? IngredientStatus.OutOfStock
                                : ingredient.MinStockLevel > 0 && ingredient.InventoryStock.CurrentQuantity <= ingredient.MinStockLevel
                                    ? IngredientStatus.LowStock
                                    : IngredientStatus.InStock;
                        }

                        foreach (Models.Orders.OrderDetail detail in preparingOrderDetails)
                        {
                            detail.ItemStatusId = cancelledStatus.Id;
                            Repo.Update(detail, userId);
                        }
                    }
                }
            }

            Repo.Update(order, userId);
            await Repo.SaveAsync();

            return true;
        }
        public async Task<bool> UpdateOrderDetailStatus(Guid orderDetailId, int statusId, string userId)
        {
            Models.Orders.OrderDetail? orderDetail = await Repo.GetOneAsync<Models.Orders.OrderDetail>(
                filter: od => od.Id == orderDetailId,
                includeProperties: "Dish,Order");

            if (orderDetail == null)
            {
                return false;
            }

            List<RestX.BLL.DataTranferObjects.Status.StatusValues> orderDetailStatuses =
                (await statusValueService.GetStatuses("order-detail")).ToList();

            bool statusExists = orderDetailStatuses.Any(x => x.Id == statusId);
            if (!statusExists)
            {
                throw new AppException("Invalid order detail status.");
            }

            int oldStatusId = orderDetail.ItemStatusId;

            RestX.BLL.DataTranferObjects.Status.StatusValues? preparingStatus = orderDetailStatuses.FirstOrDefault(x =>
                string.Equals(x.Code, "PREPARING", StringComparison.OrdinalIgnoreCase));
            RestX.BLL.DataTranferObjects.Status.StatusValues? cancelledStatus = orderDetailStatuses.FirstOrDefault(x =>
                string.Equals(x.Code, "CANCELLED", StringComparison.OrdinalIgnoreCase));

            if (preparingStatus != null
                && oldStatusId != preparingStatus.Id
                && statusId == preparingStatus.Id)
            {
                Dictionary<Guid, int> dishQtyToDeduct = new Dictionary<Guid, int>
                {
                    [orderDetail.DishId] = orderDetail.Quantity
                };

                await ingredientService.DeductFromRecipes(dishQtyToDeduct);
            }
            else if (preparingStatus != null
                && cancelledStatus != null
                && oldStatusId == preparingStatus.Id
                && statusId == cancelledStatus.Id)
            {
                List<DishRecipe> recipes = (await Repo.GetAsync<DishRecipe>(
                    filter: r => r.DishId == orderDetail.DishId,
                    includeProperties: "Ingredient,Ingredient.InventoryStock"
                )).ToList();

                foreach (DishRecipe recipe in recipes)
                {
                    Models.Inventory.Ingredient? ingredient = recipe.Ingredient;
                    if (ingredient?.InventoryStock == null)
                    {
                        continue;
                    }

                    decimal restoreQuantity = recipe.Quantity * orderDetail.Quantity;
                    ingredient.InventoryStock.CurrentQuantity += restoreQuantity;
                    ingredient.InventoryStock.LastUpdated = DateTime.UtcNow.AddHours(7);

                    ingredient.Status = ingredient.InventoryStock.CurrentQuantity == 0
                        ? IngredientStatus.OutOfStock
                        : ingredient.MinStockLevel > 0 && ingredient.InventoryStock.CurrentQuantity <= ingredient.MinStockLevel
                            ? IngredientStatus.LowStock
                            : IngredientStatus.InStock;
                }
            }

            if (cancelledStatus != null
                && oldStatusId != cancelledStatus.Id
                && statusId == cancelledStatus.Id
                && orderDetail.Order != null)
            {
                decimal cancelledAmount = orderDetail.Quantity * orderDetail.UnitPrice;

                orderDetail.Order.SubTotal -= cancelledAmount;
                if (orderDetail.Order.SubTotal < 0)
                {
                    orderDetail.Order.SubTotal = 0;
                }

                orderDetail.Order.CalculateTotalAmount();
            }

            orderDetail.ItemStatusId = statusId;
            Repo.Update(orderDetail, userId);

            if (orderDetail.Order != null)
            {
                Repo.Update(orderDetail.Order, userId);
            }

            await Repo.SaveAsync();

            return true;
        }
        public async Task<IEnumerable<DataTranferObjects.Orders.OrderDetail>> GetAllOrderDetails()
        {
            var orderDetailStatuses = await statusValueService.GetStatuses("order-detail");
            var preparingStatus = orderDetailStatuses.FirstOrDefault(x => x.Code == "PREPARING");

            if (preparingStatus == null)
                return Enumerable.Empty<DataTranferObjects.Orders.OrderDetail>();

            var startOfDay = DateTime.UtcNow.AddHours(7).Date;

            var now = DateTime.UtcNow.AddHours(7);

            var orderDetails = (await Repo.GetAsync<Models.Orders.OrderDetail>(
                filter: od => od.ItemStatusId == preparingStatus.Id
                              && od.CreatedDate >= startOfDay
                              && od.Order != null
                              && od.Order.OrderStatusId == (int)OrderStatus.Open
                              && od.Order.TableSessions.Any(ts =>
                                      ts.IsActive &&
                                      ts.StartedAt <= now
                              ),
                orderBy: query => query.OrderBy(od => od.CreatedDate),
                includeProperties: "ItemStatus,Dish,Order,Order.TableSessions,Order.TableSessions.Table"
            )).ToList();

            var mappedDetails = mapper.Map<List<DataTranferObjects.Orders.OrderDetail>>(orderDetails);

            for (int i = 0; i < orderDetails.Count; i++)
            {
                var entity = orderDetails[i];
                var dto = mappedDetails[i];

                if (entity.Order?.TableSessions != null && entity.Order.TableSessions.Any())
                {
                    dto.TableCode = entity.Order.TableSessions
                        .Where(ts => ts.Table != null && !string.IsNullOrEmpty(ts.Table.Code))
                        .Select(ts => ts.Table.Code)
                        .ToList();
                }
                else
                {
                    dto.TableCode = new List<string>();
                }
            }

            return mappedDetails;
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
        private async Task<int> GetPreparingOrderDetailStatusId()
        {
            IEnumerable<RestX.BLL.DataTranferObjects.Status.StatusValues> statuses =
                await statusValueService.GetStatuses("order-detail");

            RestX.BLL.DataTranferObjects.Status.StatusValues? preparingStatus = statuses.FirstOrDefault(
                x => string.Equals(x.Code, "PREPARING", StringComparison.OrdinalIgnoreCase));

            if (preparingStatus == null)
            {
                throw new AppException("Status 'PREPARING' for 'order-detail' was not found.");
            }

            return preparingStatus.Id;
        }
        
        //private List<Models.Orders.OrderDetail> GroupOrderDetailsByDish(IEnumerable<Models.Orders.OrderDetail> orderDetails)
        //{
        //    if (orderDetails == null || !orderDetails.Any())
        //    {
        //        return new List<Models.Orders.OrderDetail>();
        //    }

        //    return orderDetails
        //        .GroupBy(d => new { d.DishId, d.ItemStatusId })
        //        .Select(dishGroup =>
        //        {
        //            var firstItem = dishGroup.First();

        //            firstItem.Quantity = dishGroup.Sum(x => x.Quantity);

        //            var notes = dishGroup
        //                .Where(x => !string.IsNullOrWhiteSpace(x.Note))
        //                .Select(x => x.Note.Trim())
        //                .Distinct();
        //            firstItem.Note = string.Join("; ", notes);

        //            return firstItem;
        //        })
        //        .ToList();
        //}

        public async Task<DataTranferObjects.Orders.Order?> GetOrderByTableId(Guid tableId)
        {
            var now = DateTime.UtcNow.AddHours(7);

            var activeSessions = (await Repo.GetAsync<Models.Reservations.TableSession>(
                    filter: ts => ts.TableId == tableId
                               && ts.IsActive
                               && ts.StartedAt <= now,
                    orderBy: query => query.OrderByDescending(ts => ts.StartedAt)
                )).ToList();

            if (!activeSessions.Any())
            {
                return null;
            }

            var sessionWithOrder = activeSessions.FirstOrDefault(ts => ts.OrderId.HasValue);

            if (sessionWithOrder == null)
            {
                return null;
            }

            return await GetOrderById(sessionWithOrder.OrderId!.Value);
        }

        public async Task<DataTranferObjects.Orders.Order> PreOrderByReservation(
            Guid reservationId,
            DataTranferObjects.Orders.Order order,
            string userId)
        {
            if (reservationId == Guid.Empty)
            {
                throw new AppException("ReservationId is required.");
            }

            Models.Reservations.Reservation? reservation = await Repo.GetOneAsync<Models.Reservations.Reservation>(
                filter: r => r.Id == reservationId,
                includeProperties: "ReservationStatus,TableSessions"
            );

            if (reservation == null)
            {
                throw new AppException("Reservation not found.");
            }

            string reservationStatusCode = reservation.ReservationStatus?.Code ?? string.Empty;
            if (string.Equals(reservationStatusCode, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                throw new AppException("Cannot pre-order for a cancelled reservation.");
            }

            if (reservation.CheckedInAt.HasValue)
            {
                throw new AppException("Reservation already checked in. Please order by table/session flow.");
            }

            DateTime now = DateTime.UtcNow.AddHours(7);
            if (reservation.Time <= now)
            {
                throw new AppException("Reservation time has started. Please order by table/session flow.");
            }

            List<Models.Reservations.TableSession> sessions = (await Repo.GetAsync<Models.Reservations.TableSession>(
                filter: ts => ts.ReservationId == reservationId && ts.IsActive
            )).ToList();

            if (!sessions.Any())
            {
                throw new AppException("No active table session found for this reservation.");
            }

            Models.Orders.Order? existingOrder = await Repo.GetFirstAsync<Models.Orders.Order>(
                filter: o => o.ReservationId == reservationId,
                orderBy: q => q.OrderByDescending(o => o.CreatedDate)
            );

            if (existingOrder != null && existingOrder.OrderStatusId == (int)OrderStatus.Completed)
            {
                throw new AppException("Reservation order was already completed.");
            }

            order.CustomerId = reservation.CustomerId;
            order.ReservationId = reservationId;

            Models.Orders.Order savedOrder;

            if (existingOrder != null && existingOrder.OrderStatusId != (int)OrderStatus.Cancelled)
            {
                order.Id = existingOrder.Id;
                order.Reference ??= existingOrder.Reference;
                order.DiscountAmount ??= existingOrder.DiscountAmount;
                order.TaxAmount ??= existingOrder.TaxAmount;
                order.ServiceCharge ??= existingOrder.ServiceCharge;
                order.CompletedAt ??= existingOrder.CompletedAt;
                order.CancelledAt ??= existingOrder.CancelledAt;
                order.HandledBy ??= existingOrder.HandledBy;

                await UpdateOrder(existingOrder.Id, order, userId);

                savedOrder = await Repo.GetByIdAsync<Models.Orders.Order>(existingOrder.Id)
                    ?? throw new AppException("Order not found after update.");
            }
            else
            {
                order.Id = null;
                savedOrder = await UpsertOrder(order, userId);
            }

            foreach (Models.Reservations.TableSession session in sessions)
            {
                if (session.OrderId != savedOrder.Id)
                {
                    session.OrderId = savedOrder.Id;
                    Repo.Update(session, userId);
                }
            }

            await Repo.SaveAsync();

            DataTranferObjects.Orders.Order? result = await GetOrderById(savedOrder.Id);
            return result ?? mapper.Map<DataTranferObjects.Orders.Order>(savedOrder);
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
                sheet.Cells[row, 2].Value = o.CustomerName ?? "";
                sheet.Cells[row, 3].Value = o.CustomerEmail ?? "";
                sheet.Cells[row, 4].Value = o.OrderStatusId.ToString();
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
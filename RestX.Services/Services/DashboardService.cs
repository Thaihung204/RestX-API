using Microsoft.Data.SqlClient;
using RestX.BLL.DataTranferObjects.Dashboard;
using RestX.BLL.Interfaces;
using RestX.DAL.Context;
using RestX.Models.Enum;
using RestX.Models.Tenants;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TableStatusEnum = RestX.Models.Enum.TableStatus;

namespace RestX.BLL.Services
{
    public class DashboardService : BaseService, IDashboardService
    {
        public DashboardService(IRepository repo, IRedisService redisService, IEnumerable<ActiveTenant> tenant)
            : base(repo, redisService, tenant)
        {
        }

        public async Task<DashboardSummary> GetSummaryAsync(DashboardRequest request)
        {
            var (fromDate, toDate) = CalculateDateRange(request);
            var periodLength = (int)(toDate - fromDate).TotalDays;
            var prevFromDate = fromDate.AddDays(-periodLength);
            var prevToDate = fromDate;

            var summary = new DashboardSummary
            {
                FromDate = fromDate,
                ToDate = toDate
            };
            var currentRevenueParams = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate },
                new("status", SqlDbType.Int) { Value = (int)PaymentStatus.Success },
                new("purpose", SqlDbType.Int) { Value = (int)PaymentPurpose.Order }
            };
            var currentRevenue = await Repo.ExecuteSqlCommandAsync<decimal?>(
                @"SELECT ISNULL(SUM(p.Amount), 0)
                   FROM Payments p
                   WHERE p.CreatedDate >= @from AND p.CreatedDate < @to
                   AND p.Status = @status AND p.Purpose = @purpose",
                currentRevenueParams.Cast<object>().ToArray());

            var prevRevenueParams = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = prevFromDate },
                new("to", SqlDbType.DateTime2) { Value = prevToDate },
                new("status", SqlDbType.Int) { Value = (int)PaymentStatus.Success },
                new("purpose", SqlDbType.Int) { Value = (int)PaymentPurpose.Order }
            };
            var prevRevenue = await Repo.ExecuteSqlCommandAsync<decimal?>(
                @"SELECT ISNULL(SUM(p.Amount), 0)
                   FROM Payments p
                   WHERE p.CreatedDate >= @from AND p.CreatedDate < @to
                   AND p.Status = @status AND p.Purpose = @purpose",
                prevRevenueParams.Cast<object>().ToArray());

            summary.Revenue.Total = currentRevenue ?? 0;
            summary.Revenue.ChangePercent = CalculateChangePercent(prevRevenue ?? 0, currentRevenue ?? 0);

            var orderParams = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var orderStatusQuery = @"
                SELECT
                    SUM(CASE WHEN o.OrderStatusId = 0 THEN 1 ELSE 0 END) AS Pending,
                    SUM(CASE WHEN o.OrderStatusId = 1 THEN 1 ELSE 0 END) AS Confirmed,
                    SUM(CASE WHEN o.OrderStatusId = 2 THEN 1 ELSE 0 END) AS Serving,
                    SUM(CASE WHEN o.OrderStatusId = 3 THEN 1 ELSE 0 END) AS Completed,
                    SUM(CASE WHEN o.OrderStatusId = 4 THEN 1 ELSE 0 END) AS Cancelled
                FROM Orders o
                WHERE o.CreatedDate >= @from AND o.CreatedDate < @to";

            var orderStats = await Repo.ExecuteSqlSelectAsync<QueryResult.OrderStatusCount>(
                orderStatusQuery,
                orderParams.Cast<object>().ToArray());

            var orderStat = orderStats.FirstOrDefault();
            summary.Orders.Total = await GetOrderCountInRangeAsync(fromDate, toDate);
            summary.Orders.Pending = orderStat?.Pending ?? 0;
            summary.Orders.Confirmed = orderStat?.Confirmed ?? 0;
            summary.Orders.Processing = orderStat?.Serving ?? 0;
            summary.Orders.Completed = orderStat?.Completed ?? 0;
            summary.Orders.Cancelled = orderStat?.Cancelled ?? 0;

            // Live Processing Orders - orders đang xử lý ngay bây giờ (Pending=0, Confirmed=1, Serving=2)
            var liveProcessing = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(o.Id)
                   FROM Orders o
                   WHERE o.OrderStatusId IN (0, 1, 2)",
                null);

            summary.Orders.LiveProcessing = liveProcessing ?? 0;

            // Reservations
            var resParams = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var reservationStats = await Repo.ExecuteSqlSelectAsync<QueryResult.StatusCount>(
                @"SELECT rs.Code AS Status, COUNT(r.Id) AS Count
                   FROM Reservations r
                   LEFT JOIN StatusValues rs ON r.ReservationStatusId = rs.Id
                   WHERE r.CreatedDate >= @from AND r.CreatedDate < @to
                   GROUP BY rs.Code",
                resParams.Cast<object>().ToArray());

            summary.Reservations.Total = await GetReservationCountInRangeAsync(fromDate, toDate);
            summary.Reservations.PendingDeposit = GetStatusCount(reservationStats, "DEPOSIT_PENDING");
            summary.Reservations.Confirmed = GetStatusCount(reservationStats, "CONFIRMED");
            summary.Reservations.Completed = GetStatusCount(reservationStats, "COMPLETED");
            summary.Reservations.Cancelled = GetStatusCount(reservationStats, "CANCELLED");
            summary.Reservations.NoShow = GetStatusCount(reservationStats, "NO_SHOW");

            // Live Serving Reservations - đặt bàn đang phục vụ ngay bây giờ
            var checkInCountList = await Repo.ExecuteSqlSelectAsync<QueryResult.CustomerCount>(
                @"SELECT COUNT(DISTINCT r.Id) AS Count
                   FROM Reservations r
                   WHERE r.CheckedInAt IS NOT NULL
                   AND (SELECT COUNT(*) FROM TableSessions ts WHERE ts.ReservationId = r.Id AND ts.EndedAt IS NULL) > 0",
                null);

            summary.Reservations.LiveServing = checkInCountList.FirstOrDefault()?.Count ?? 0;

            // New Customers
            var currentNewCustomers = await GetNewCustomerCountAsync(fromDate, toDate);
            var prevNewCustomers = await GetNewCustomerCountAsync(prevFromDate, prevToDate);

            summary.NewCustomers.Total = currentNewCustomers;
            summary.NewCustomers.ChangePercent = CalculateChangePercent(prevNewCustomers, currentNewCustomers);

            return summary;
        }

        public async Task<RevenueTrend> GetRevenueTrendAsync(DashboardRequest request)
        {
            var (fromDate, toDate) = CalculateDateRange(request);
            var dto = new RevenueTrend
            {
                FilterType = request.FilterType,
                FromDate = fromDate,
                ToDate = toDate
            };

            var sql = GetRevenueTrendSql(request.FilterType);
            var @params = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate },
                new("status", SqlDbType.Int) { Value = (int)PaymentStatus.Success },
                new("purpose", SqlDbType.Int) { Value = (int)PaymentPurpose.Order }
            };
            var data = await Repo.ExecuteSqlSelectAsync<QueryResult.TrendPoint>(sql, @params.Cast<object>().ToArray());

            dto.TotalRevenue = data.Sum(x => x.Value ?? 0);

            var trendDict = new Dictionary<string, decimal>();
            foreach (var point in data)
            {
                var dateStr = point.Date.ToString("yyyy-MM-dd");
                trendDict[dateStr] = (decimal)point.Value;
            }

            var current = fromDate;
            while (current < toDate)
            {
                var lookupDate = request.FilterType?.ToLower() == "year"
                    ? new DateTime(current.Year, current.Month, 1)
                    : current;
                var dateStr = lookupDate.ToString("yyyy-MM-dd");
                var label = GenerateLabel(current, request.FilterType);
                var value = trendDict.ContainsKey(dateStr) ? trendDict[dateStr] : 0;

                dto.RevenueTrends.Add(new RevenueTrend.TrendPoint
                {
                    Label = label,
                    Date = dateStr,
                    Value = value
                });
                if (request.FilterType?.ToLower() == "year")
                    current = current.AddMonths(1);
                else
                    current = current.AddDays(1);
            }

            return dto;
        }

        public async Task<OrderTrend> GetOrderTrendAsync(DashboardRequest request)
        {
            var (fromDate, toDate) = CalculateDateRange(request);
            var dto = new OrderTrend
            {
                FilterType = request.FilterType,
                FromDate = fromDate,
                ToDate = toDate
            };

            var sql = GetOrderTrendSql(request.FilterType);
            var @params = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var data = await Repo.ExecuteSqlSelectAsync<QueryResult.TrendPoint>(sql, @params.Cast<object>().ToArray());

            dto.TotalOrders = data.Sum(x => x.Total ?? 0);

            var trendDict = new Dictionary<string, int>();
            foreach (var point in data)
            {
                var dateStr = point.Date.ToString("yyyy-MM-dd");
                trendDict[dateStr] = (int)point.Total;
            }

            var current = fromDate;
            while (current < toDate)
            {
                var lookupDate = request.FilterType?.ToLower() == "year"
                    ? new DateTime(current.Year, current.Month, 1)
                    : current;
                var dateStr = lookupDate.ToString("yyyy-MM-dd");
                var label = GenerateLabel(current, request.FilterType);
                var total = trendDict.ContainsKey(dateStr) ? trendDict[dateStr] : 0;

                dto.OrderTrends.Add(new OrderTrend.TrendPoint
                {
                    Label = label,
                    Date = dateStr,
                    Total = total
                });

                if (request.FilterType?.ToLower() == "year")
                    current = current.AddMonths(1);
                else
                    current = current.AddDays(1);
            }

            return dto;
        }

        public async Task<TopDish> GetTopDishesAsync(DashboardRequest request, int top = 5, string sortBy = "revenue")
        {
            var (fromDate, toDate) = CalculateDateRange(request);
            var dto = new TopDish
            {
                FromDate = fromDate,
                ToDate = toDate
            };

            var orderByClause = sortBy.ToLower() == "quantity" ? "SUM(od.Quantity)" : "SUM(od.Quantity * d.Price)";

            var sql = $@"SELECT TOP {top}
                       d.Id AS DishId,
                       d.Name,
                       SUM(od.Quantity) AS Quantity,
                       CAST(SUM(od.Quantity * d.Price) AS DECIMAL(18,2)) AS Revenue
                   FROM OrderDetails od
                   JOIN Orders o ON od.OrderId = o.Id
                   JOIN Dishes d ON od.DishId = d.Id
                   WHERE o.CreatedDate >= @from AND o.CreatedDate < @to
                   GROUP BY d.Id, d.Name
                   ORDER BY {orderByClause} DESC, NEWID()";

            var @params = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var data = await Repo.ExecuteSqlSelectAsync<QueryResult.DishItem>(sql, @params.Cast<object>().ToArray());

            foreach (var item in data)
            {
                dto.Dishes.Add(new TopDish.DishItem
                {
                    DishId = item.DishId,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Revenue = item.Revenue
                });
            }

            return dto;
        }

        public async Task<DataTranferObjects.Dashboard.TableStatus> GetTableStatusAsync()
        {
            var @params = new List<SqlParameter>
            {
                new("available", SqlDbType.Int) { Value = (int)TableStatusEnum.Available },
                new("occupied", SqlDbType.Int) { Value = (int)TableStatusEnum.Occupied },
                new("reserved", SqlDbType.Int) { Value = (int)TableStatusEnum.Reserved }
            };
            var data = await Repo.ExecuteSqlSelectAsync<QueryResult.TableCount>(
                @"SELECT
                       COUNT(*) AS Total,
                       SUM(CASE WHEN t.TableStatusId = @available THEN 1 ELSE 0 END) AS Available,
                       SUM(CASE WHEN t.TableStatusId = @occupied THEN 1 ELSE 0 END) AS Occupied,
                       SUM(CASE WHEN t.TableStatusId = @reserved THEN 1 ELSE 0 END) AS Reserved
                   FROM Tables t",
                @params.Cast<object>().ToArray());

            var result = data.FirstOrDefault();
            return new DataTranferObjects.Dashboard.TableStatus
            {
                Total = result?.Total ?? 0,
                Available = result?.Available ?? 0,
                Occupied = result?.Occupied ?? 0,
                Reserved = result?.Reserved ?? 0
            };
        }

        public async Task<CustomerStats> GetCustomerStatsAsync(DashboardRequest request)
        {
            var (fromDate, toDate) = CalculateDateRange(request);
            var prevFromDate = GetPreviousPeriodStart(fromDate, request.FilterType);
            var prevToDate = fromDate;

            var dto = new CustomerStats
            {
                FromDate = fromDate,
                ToDate = toDate
            };

            // New customers
            dto.NewCustomers = await GetNewCustomerCountAsync(fromDate, toDate);
            var prevNewCustomers = await GetNewCustomerCountAsync(prevFromDate, prevToDate);
            dto.ChangePercent = CalculateChangePercent(prevNewCustomers, dto.NewCustomers);

            // Returning customers
            var returningParams = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var returningData = await Repo.ExecuteSqlSelectAsync<QueryResult.CustomerCount>(
                @"SELECT COUNT(DISTINCT c.Id) AS Count
                   FROM Customers c
                   WHERE (SELECT COUNT(*) FROM Orders o WHERE o.CustomerId = c.Id AND o.CreatedDate >= @from AND o.CreatedDate < @to) > 1",
                returningParams.Cast<object>().ToArray());

            dto.ReturningCustomers = returningData.FirstOrDefault()?.Count ?? 0;

            // Total orders and revenue
            var revenueParams = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var revenueData = await Repo.ExecuteSqlSelectAsync<QueryResult.OrderRevenue>(
                @"SELECT COUNT(o.Id) AS TotalOrders, ISNULL(SUM(o.TotalAmount), 0) AS TotalRevenue
                   FROM Orders o
                   WHERE o.CreatedDate >= @from AND o.CreatedDate < @to",
                revenueParams.Cast<object>().ToArray());

            var revenue = revenueData.FirstOrDefault();
            dto.TotalOrders = revenue?.TotalOrders ?? 0;

            var totalCustomers = dto.NewCustomers + dto.ReturningCustomers;
            if (totalCustomers > 0)
            {
                dto.AverageRevenuePerCustomer = (decimal)Math.Ceiling((double)(revenue?.TotalRevenue ?? 0) / totalCustomers);
            }

            // Top 5 customers by total spending
            var topCustomersParams = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = fromDate },
                new("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var topCustomersData = await Repo.ExecuteSqlSelectAsync<QueryResult.TopCustomer>(
                @"SELECT TOP 5
                       c.Id AS CustomerId,
                       au.FullName AS CustomerName,
                       ISNULL(c.LoyaltyPoints, 0) AS LoyaltyPoints,
                       c.MembershipLevel,
                       ISNULL(SUM(o.TotalAmount), 0) AS TotalSpent
                   FROM Customers c
                   JOIN AspNetUsers au ON c.ApplicationUserId = au.Id
                   LEFT JOIN Orders o ON c.Id = o.CustomerId AND o.CreatedDate >= @from AND o.CreatedDate < @to
                   GROUP BY c.Id, au.FullName, c.LoyaltyPoints, c.MembershipLevel
                   ORDER BY TotalSpent DESC",
                topCustomersParams.Cast<object>().ToArray());

            var rank = 1;
            foreach (var customer in topCustomersData)
            {
                dto.TopCustomers.Add(new CustomerStats.TopCustomer
                {
                    Rank = rank++,
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    LoyaltyPoints = customer.LoyaltyPoints,
                    MembershipLevel = customer.MembershipLevel,
                    TotalSpent = customer.TotalSpent
                });
            }

            return dto;
        }

        // Helper methods
        private (DateTime fromDate, DateTime toDate) CalculateDateRange(DashboardRequest request)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;

            if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                // Custom date range - convert UTC to UTC+7
                var fromDate = request.FromDate.Value.ToUniversalTime().AddHours(7).Date;
                var toDate = request.ToDate.Value.ToUniversalTime().AddHours(7).AddDays(1).Date;
                return (fromDate, toDate);
            }

            // Nếu không truyền, lấy quá khứ từ now theo loại
            return request.FilterType?.ToLower() switch
            {
                "week" => (today.AddDays(-7), today.AddDays(1)),
                "month" => (today.AddDays(-30), today.AddDays(1)),
                "year" => (today.AddDays(-365), today.AddDays(1)),
                _ => (today.AddDays(-7), today.AddDays(1))
            };
        }

        private DateTime GetPreviousPeriodStart(DateTime fromDate, string filterType)
        {
            return filterType?.ToLower() switch
            {
                "week" => fromDate.AddDays(-7),
                "month" => fromDate.AddMonths(-1),
                "year" => fromDate.AddYears(-1),
                _ => fromDate.AddDays(-7)
            };
        }
        private double CalculateChangePercent(decimal previous, decimal current)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;
            return Math.Round(((double)(current - previous) / (double)previous) * 100, 2);
        }
        private double CalculateChangePercent(int previous, int current)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;
            return Math.Round(((double)(current - previous) / (double)previous) * 100, 2);
        }
        private int GetStatusCount(IEnumerable<dynamic> stats, string statusCode)
        {
            var item = stats.FirstOrDefault(s => s.Status == statusCode);
            return item != null ? (int)item.Count : 0;
        }
        private async Task<int> GetOrderCountInRangeAsync(DateTime from, DateTime to)
        {
            var @params = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = from },
                new("to", SqlDbType.DateTime2) { Value = to }
            };
            var result = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(o.Id) FROM Orders o WHERE o.CreatedDate >= @from AND o.CreatedDate < @to",
                @params.Cast<object>().ToArray());
            return result ?? 0;
        }
        private async Task<int> GetReservationCountInRangeAsync(DateTime from, DateTime to)
        {
            var @params = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = from },
                new("to", SqlDbType.DateTime2) { Value = to }
            };
            var result = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(r.Id) FROM Reservations r WHERE r.CreatedDate >= @from AND r.CreatedDate < @to",
                @params.Cast<object>().ToArray());
            return result ?? 0;
        }

        private async Task<int> GetNewCustomerCountAsync(DateTime from, DateTime to)
        {
            var @params = new List<SqlParameter>
            {
                new("from", SqlDbType.DateTime2) { Value = from },
                new("to", SqlDbType.DateTime2) { Value = to }
            };
            var result = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(c.Id) FROM Customers c WHERE c.CreatedDate >= @from AND c.CreatedDate < @to",
                @params.Cast<object>().ToArray());
            return result ?? 0;
        }
        private string GetRevenueTrendSql(string filterType)
        {
            return filterType?.ToLower() switch
            {
                "month" => @"
                    SELECT
                        CAST(CONVERT(VARCHAR(10), p.PaymentDate, 23) AS DATE) AS Date,
                        CAST(SUM(p.Amount) AS DECIMAL(18,2)) AS Value
                    FROM Payments p
                    WHERE p.CreatedDate >= @from AND p.CreatedDate < @to
                    AND p.Status = @status AND p.Purpose = @purpose
                    GROUP BY CAST(CONVERT(VARCHAR(10), p.PaymentDate, 23) AS DATE)
                    ORDER BY Date",

                "year" => @"
                    SELECT
                        DATEFROMPARTS(YEAR(p.PaymentDate), MONTH(p.PaymentDate), 1) AS Date,
                        CAST(SUM(p.Amount) AS DECIMAL(18,2)) AS Value
                    FROM Payments p
                    WHERE p.CreatedDate >= @from AND p.CreatedDate < @to
                    AND p.Status = @status AND p.Purpose = @purpose
                    GROUP BY YEAR(p.PaymentDate), MONTH(p.PaymentDate)
                    ORDER BY Date",

                _ => @"
                    SELECT
                        CAST(CONVERT(VARCHAR(10), p.PaymentDate, 23) AS DATE) AS Date,
                        CAST(SUM(p.Amount) AS DECIMAL(18,2)) AS Value
                    FROM Payments p
                    WHERE p.CreatedDate >= @from AND p.CreatedDate < @to
                    AND p.Status = @status AND p.Purpose = @purpose
                    GROUP BY CAST(CONVERT(VARCHAR(10), p.PaymentDate, 23) AS DATE)
                    ORDER BY Date"
            };
        }
        private string GetOrderTrendSql(string filterType)
        {
            return filterType?.ToLower() switch
            {
                "month" => @"
                    SELECT
                        CAST(CONVERT(VARCHAR(10), o.CreatedDate, 23) AS DATE) AS Date,
                        COUNT(o.Id) AS Total
                    FROM Orders o
                    WHERE o.CreatedDate >= @from AND o.CreatedDate < @to
                    GROUP BY CAST(CONVERT(VARCHAR(10), o.CreatedDate, 23) AS DATE)
                    ORDER BY Date",

                "year" => @"
                    SELECT
                        DATEFROMPARTS(YEAR(o.CreatedDate), MONTH(o.CreatedDate), 1) AS Date,
                        COUNT(o.Id) AS Total
                    FROM Orders o
                    WHERE o.CreatedDate >= @from AND o.CreatedDate < @to
                    GROUP BY YEAR(o.CreatedDate), MONTH(o.CreatedDate)
                    ORDER BY Date",

                _ => @"
                    SELECT
                        CAST(CONVERT(VARCHAR(10), o.CreatedDate, 23) AS DATE) AS Date,
                        COUNT(o.Id) AS Total
                    FROM Orders o
                    WHERE o.CreatedDate >= @from AND o.CreatedDate < @to
                    GROUP BY CAST(CONVERT(VARCHAR(10), o.CreatedDate, 23) AS DATE)
                    ORDER BY Date"
            };
        }
        private string GenerateLabel(object dateObj, string filterType)
        {
            if (!(dateObj is DateTime date))
                return string.Empty;

            return filterType?.ToLower() switch
            {
                "week" => new[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" }[(int)date.DayOfWeek],
                "month" => date.Day.ToString("D2"),
                "year" => new[] { "Th1", "Th2", "Th3", "Th4", "Th5", "Th6", "Th7", "Th8", "Th9", "Th10", "Th11", "Th12" }[date.Month - 1],
                _ => string.Empty
            };
        }
    }
}

using Microsoft.Data.SqlClient;
using RestX.BLL.DataTranferObjects.Dashboard;
using RestX.BLL.Interfaces;
using RestX.Models.Enum;
using RestX.Models.Tenants;
using System.Data;
using TableStatusEnum = RestX.Models.Enum.TableStatus;

namespace RestX.BLL.Services
{
    public class DashboardService : BaseService, IDashboardService
    {
        public DashboardService(IRepository repo, IRedisService redisService, IEnumerable<ActiveTenant> tenant)
            : base(repo, redisService, tenant)
        {
        }

        public async Task<DashboardOverview> GetOverviewAsync(DashboardRequest request, int top = 5, string sortBy = "revenue")
        {
            var summary = await GetSummaryAsync(request);
            var revenueTrend = await GetRevenueTrendAsync(request);
            var orderTrend = await GetOrderTrendAsync(request);
            var topDishes = await GetTopDishesAsync(request, top, sortBy);
            var tableStatus = await GetTableStatusAsync();
            var customerStats = await GetCustomerStatsAsync(request);

            return new DashboardOverview
            {
                Summary = summary,
                RevenueTrend = revenueTrend,
                OrderTrend = orderTrend,
                TopDishes = topDishes,
                TableStatus = tableStatus,
                CustomerStats = customerStats
            };
        }

        public async Task<DashboardSummary> GetSummaryAsync(DashboardRequest request)
        {
            var (fromDate, toDate) = CalculateDateRange(request);
            var prevFromDate = GetPreviousPeriodStart(fromDate, request.FilterType);
            var prevToDate = fromDate;

            var summary = new DashboardSummary
            {
                FromDate = fromDate,
                ToDate = toDate
            };
            var currentRevenue = await Repo.ExecuteSqlCommandAsync<decimal?>(@"SELECT ISNULL(SUM(p.Amount), 0)
                FROM Payments p
                WHERE p.PaymentDate >= @from AND p.PaymentDate < @to
                AND p.Status = @status AND p.Purpose = @purpose",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate },
                    new SqlParameter("status", SqlDbType.Int) { Value = (int)PaymentStatus.Success },
                    new SqlParameter("purpose", SqlDbType.Int) { Value = (int)PaymentPurpose.Order }
                });

            var prevRevenue = await Repo.ExecuteSqlCommandAsync<decimal?>(@"SELECT ISNULL(SUM(p.Amount), 0)
                FROM Payments p
                WHERE p.PaymentDate >= @from AND p.PaymentDate < @to
                AND p.Status = @status AND p.Purpose = @purpose",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = prevFromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = prevToDate },
                    new SqlParameter("status", SqlDbType.Int) { Value = (int)PaymentStatus.Success },
                    new SqlParameter("purpose", SqlDbType.Int) { Value = (int)PaymentPurpose.Order }
                });

            var orderStatsResult = await Repo.ExecuteSqlSelectAsync<QueryResult.OrderStatusCount>(@"
                SELECT
                    SUM(CASE WHEN o.OrderStatusId = 0 THEN 1 ELSE 0 END) AS [Open],
                    SUM(CASE WHEN o.OrderStatusId = 1 THEN 1 ELSE 0 END) AS Completed,
                    SUM(CASE WHEN o.OrderStatusId = 2 THEN 1 ELSE 0 END) AS Cancelled,
                    COUNT(o.Id) AS Total
                FROM Orders o
                WHERE o.CreatedDate >= @from AND o.CreatedDate < @to",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
                });

            var reservationStatsResult = await Repo.ExecuteSqlSelectAsync<QueryResult.StatusCount>(@"
                SELECT rs.Code AS Status, COUNT(r.Id) AS Count
                FROM Reservations r
                LEFT JOIN StatusValues rs ON r.ReservationStatusId = rs.Id
                WHERE r.CreatedDate >= @from AND r.CreatedDate < @to
                GROUP BY rs.Code",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
                });

            var liveProcessing = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(o.Id) FROM Orders o WHERE o.OrderStatusId = 0", null);

            var liveServingResult = await Repo.ExecuteSqlSelectAsync<QueryResult.CustomerCount>(@"
                SELECT COUNT(DISTINCT r.Id) AS Count
                FROM Reservations r
                WHERE r.CheckedInAt IS NOT NULL
                AND (SELECT COUNT(*) FROM TableSessions ts WHERE ts.ReservationId = r.Id AND ts.EndedAt IS NULL) > 0",
                null);

            var currentNewCustomers = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(c.Id) FROM Customers c WHERE c.CreatedDate >= @from AND c.CreatedDate < @to",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
                });

            var prevNewCustomers = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(c.Id) FROM Customers c WHERE c.CreatedDate >= @from AND c.CreatedDate < @to",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = prevFromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = prevToDate }
                });

            // Revenue
            summary.Revenue.Total = currentRevenue ?? 0;
            summary.Revenue.ChangePercent = CalculateChangePercent((double)(prevRevenue ?? 0), (double)(currentRevenue ?? 0));

            // Orders — Total comes from the same query, no extra round-trip
            var orderStat = orderStatsResult.FirstOrDefault();
            summary.Orders.Total = orderStat?.Total ?? 0;
            summary.Orders.Open = orderStat?.Open ?? 0;
            summary.Orders.Completed = orderStat?.Completed ?? 0;
            summary.Orders.Cancelled = orderStat?.Cancelled ?? 0;
            summary.Orders.LiveProcessing = liveProcessing ?? 0;

            // Reservations
            summary.Reservations.Total = GetStatusCountTotal(reservationStatsResult);
            summary.Reservations.Pending = GetStatusCount(reservationStatsResult, "PENDING");
            summary.Reservations.Confirmed = GetStatusCount(reservationStatsResult, "CONFIRMED");
            summary.Reservations.Cancelled = GetStatusCount(reservationStatsResult, "CANCELLED");
            summary.Reservations.LiveServing = liveServingResult.FirstOrDefault()?.Count ?? 0;

            // New Customers
            summary.NewCustomers.Total = currentNewCustomers ?? 0;
            summary.NewCustomers.ChangePercent = CalculateChangePercent(prevNewCustomers ?? 0, currentNewCustomers ?? 0);

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
            var @params = new[]
            {
                new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate },
                new SqlParameter("status", SqlDbType.Int) { Value = (int)PaymentStatus.Success },
                new SqlParameter("purpose", SqlDbType.Int) { Value = (int)PaymentPurpose.Order }
            };
            var data = await Repo.ExecuteSqlSelectAsync<QueryResult.TrendPoint>(sql, @params.Cast<object>().ToArray());

            dto.TotalRevenue = data.Sum(x => x.Value ?? 0);

            var filterLower = request.FilterType?.ToLower();
            var isYear = filterLower == "year";
            var isToday = filterLower == "today";

            var keyFormat = isToday ? "yyyy-MM-dd HH:00" : "yyyy-MM-dd";
            var trendDict = data.ToDictionary(
                p => p.Date.ToString(keyFormat),
                p => p.Value ?? 0);
            var current = fromDate;
            while (current < toDate)
            {
                var dateStr = isToday
                    ? current.ToString("yyyy-MM-dd HH:00")
                    : isYear
                        ? new DateTime(current.Year, current.Month, 1).ToString("yyyy-MM-dd")
                        : current.ToString("yyyy-MM-dd");

                dto.RevenueTrends.Add(new RevenueTrend.TrendPoint
                {
                    Label = GenerateLabel(current, request.FilterType),
                    Date = dateStr,
                    Value = trendDict.GetValueOrDefault(dateStr)
                });

                current = isToday ? current.AddHours(1) : isYear ? current.AddMonths(1) : current.AddDays(1);
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
            var @params = new[]
            {
                new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var data = await Repo.ExecuteSqlSelectAsync<QueryResult.TrendPoint>(sql, @params.Cast<object>().ToArray());

            dto.TotalOrders = data.Sum(x => x.Total ?? 0);

            var filterLower = request.FilterType?.ToLower();
            var isYear = filterLower == "year";
            var isToday = filterLower == "today";

            var keyFormat = isToday ? "yyyy-MM-dd HH:00" : "yyyy-MM-dd";
            var trendDict = data.ToDictionary(
                p => p.Date.ToString(keyFormat),
                p => p.Total ?? 0);
            var current = fromDate;
            while (current < toDate)
            {
                var dateStr = isToday
                    ? current.ToString("yyyy-MM-dd HH:00")
                    : isYear
                        ? new DateTime(current.Year, current.Month, 1).ToString("yyyy-MM-dd")
                        : current.ToString("yyyy-MM-dd");

                dto.OrderTrends.Add(new OrderTrend.TrendPoint
                {
                    Label = GenerateLabel(current, request.FilterType),
                    Date = dateStr,
                    Total = trendDict.GetValueOrDefault(dateStr)
                });

                current = isToday ? current.AddHours(1) : isYear ? current.AddMonths(1) : current.AddDays(1);
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

            var orderByClause = sortBy.ToLower() == "quantity"
                ? "SUM(od.Quantity)"
                : "SUM(od.Quantity * d.Price)";
            var safeTop = Math.Clamp(top, 1, 50);

            var sql = $@"SELECT TOP {safeTop}
                       d.Id AS DishId,
                       d.Name,
                       SUM(od.Quantity) AS Quantity,
                       CAST(SUM(od.Quantity * d.Price) AS DECIMAL(18,2)) AS Revenue
                   FROM OrderDetails od
                   JOIN Orders o ON od.OrderId = o.Id
                   JOIN Dishes d ON od.DishId = d.Id
                   WHERE o.CreatedDate >= @from AND o.CreatedDate < @to
                   GROUP BY d.Id, d.Name
                   ORDER BY {orderByClause} DESC, d.Name ASC";

            var @params = new[]
            {
                new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
            };
            var data = await Repo.ExecuteSqlSelectAsync<TopDish.DishItem>(sql, @params.Cast<object>().ToArray());

            if (data.Count == 0)
            {
                dto.IsFallback = true;
                var fallbackSql = $@"SELECT TOP {safeTop}
                       d.Id AS DishId,
                       d.Name,
                       SUM(od.Quantity) AS Quantity,
                       CAST(SUM(od.Quantity * d.Price) AS DECIMAL(18,2)) AS Revenue
                   FROM OrderDetails od
                   JOIN Orders o ON od.OrderId = o.Id
                   JOIN Dishes d ON od.DishId = d.Id
                   GROUP BY d.Id, d.Name
                   ORDER BY {orderByClause} DESC, d.Name ASC";
                data = await Repo.ExecuteSqlSelectAsync<TopDish.DishItem>(fallbackSql, null);
            }

            dto.Dishes.AddRange(data);

            return dto;
        }

        public async Task<DataTranferObjects.Dashboard.TableStatus> GetTableStatusAsync()
        {
            var @params = new[]
            {
                new SqlParameter("available", SqlDbType.Int) { Value = (int)TableStatusEnum.Available },
                new SqlParameter("occupied", SqlDbType.Int) { Value = (int)TableStatusEnum.Occupied }
            };
            var data = await Repo.ExecuteSqlSelectAsync<QueryResult.TableCount>(
                @"SELECT
                       COUNT(*) AS Total,
                       SUM(CASE WHEN t.TableStatusId = @available THEN 1 ELSE 0 END) AS Available,
                       SUM(CASE WHEN t.TableStatusId = @occupied THEN 1 ELSE 0 END) AS Occupied
                   FROM Tables t",
                @params.Cast<object>().ToArray());

            var result = data.FirstOrDefault();
            return new DataTranferObjects.Dashboard.TableStatus
            {
                Total = result?.Total ?? 0,
                Available = result?.Available ?? 0,
                Occupied = result?.Occupied ?? 0
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

            var currentNewCustomers = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(c.Id) FROM Customers c WHERE c.CreatedDate >= @from AND c.CreatedDate < @to",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
                });

            var prevNewCustomers = await Repo.ExecuteSqlCommandAsync<int?>(
                @"SELECT COUNT(c.Id) FROM Customers c WHERE c.CreatedDate >= @from AND c.CreatedDate < @to",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = prevFromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = prevToDate }
                });

            var returningData = await Repo.ExecuteSqlSelectAsync<QueryResult.CustomerCount>(@"
                SELECT COUNT(DISTINCT c.Id) AS Count
                FROM Customers c
                WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.CustomerId = c.Id AND o.CreatedDate >= @from AND o.CreatedDate < @to)
                AND EXISTS (SELECT 1 FROM Orders o WHERE o.CustomerId = c.Id AND o.CreatedDate < @from)",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
                });

            var revenueData = await Repo.ExecuteSqlSelectAsync<QueryResult.OrderRevenue>(@"
                SELECT COUNT(o.Id) AS TotalOrders, ISNULL(SUM(o.TotalAmount), 0) AS TotalRevenue
                FROM Orders o
                WHERE o.CreatedDate >= @from AND o.CreatedDate < @to",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
                });

            // Chỉ lấy customer có thực sự chi tiêu trong kỳ (TotalSpent > 0)
            var topCustomersData = await Repo.ExecuteSqlSelectAsync<CustomerStats.TopCustomer>(@"
                SELECT TOP 5
                    c.Id AS CustomerId,
                    au.FullName AS CustomerName,
                    ISNULL(c.LoyaltyPoints, 0) AS LoyaltyPoints,
                    c.MembershipLevel,
                    ISNULL(SUM(o.TotalAmount), 0) AS TotalSpent
                FROM Customers c
                JOIN AspNetUsers au ON c.ApplicationUserId = au.Id
                JOIN Orders o ON c.Id = o.CustomerId AND o.CreatedDate >= @from AND o.CreatedDate < @to
                GROUP BY c.Id, au.FullName, c.LoyaltyPoints, c.MembershipLevel
                HAVING SUM(o.TotalAmount) > 0
                ORDER BY TotalSpent DESC",
                new object[]
                {
                    new SqlParameter("from", SqlDbType.DateTime2) { Value = fromDate },
                    new SqlParameter("to", SqlDbType.DateTime2) { Value = toDate }
                });

            if (topCustomersData.Count == 0)
            {
                dto.IsTopCustomersFallback = true;
                topCustomersData = await Repo.ExecuteSqlSelectAsync<CustomerStats.TopCustomer>(@"
                    SELECT TOP 5
                        c.Id AS CustomerId,
                        au.FullName AS CustomerName,
                        ISNULL(c.LoyaltyPoints, 0) AS LoyaltyPoints,
                        c.MembershipLevel,
                        ISNULL(SUM(o.TotalAmount), 0) AS TotalSpent
                    FROM Customers c
                    JOIN AspNetUsers au ON c.ApplicationUserId = au.Id
                    JOIN Orders o ON c.Id = o.CustomerId
                    GROUP BY c.Id, au.FullName, c.LoyaltyPoints, c.MembershipLevel
                    HAVING SUM(o.TotalAmount) > 0
                    ORDER BY TotalSpent DESC",
                    null);
            }

            dto.NewCustomers = currentNewCustomers ?? 0;
            dto.ChangePercent = CalculateChangePercent(prevNewCustomers ?? 0, dto.NewCustomers);
            dto.ReturningCustomers = returningData.FirstOrDefault()?.Count ?? 0;

            var revenue = revenueData.FirstOrDefault();
            dto.TotalOrders = revenue?.TotalOrders ?? 0;

            var totalCustomers = dto.NewCustomers + dto.ReturningCustomers;
            if (totalCustomers > 0)
                dto.AverageRevenuePerCustomer = (decimal)Math.Ceiling((double)(revenue?.TotalRevenue ?? 0) / totalCustomers);

            dto.TopCustomers.AddRange(topCustomersData.Select((c, i) => { c.Rank = i + 1; return c; }));

            return dto;
        }

        // Helper methods
        private (DateTime fromDate, DateTime toDate) CalculateDateRange(DashboardRequest request)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;

            if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                var fromDate = request.FromDate.Value.ToUniversalTime().AddHours(7).Date;
                var toDate = request.ToDate.Value.ToUniversalTime().AddHours(7).AddDays(1).Date;
                return (fromDate, toDate);
            }

            return request.FilterType?.ToLower() switch
            {
                "week" => (today.AddDays(-7), today.AddDays(1)),
                "month" => (today.AddDays(-30), today.AddDays(1)),
                "year" => (today.AddDays(-365), today.AddDays(1)),
                _ => (today, today.AddDays(1))
            };
        }

        private DateTime GetPreviousPeriodStart(DateTime fromDate, string filterType)
        {
            return filterType?.ToLower() switch
            {
                "week" => fromDate.AddDays(-7),
                "month" => fromDate.AddMonths(-1),
                "year" => fromDate.AddYears(-1),
                _ => fromDate.AddDays(-1)
            };
        }
        private double CalculateChangePercent(double previous, double current)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;
            return Math.Round((current - previous) / previous * 100, 2);
        }
        private int GetStatusCount(IEnumerable<QueryResult.StatusCount> stats, string statusCode)
        {
            return stats.FirstOrDefault(s => s.Status == statusCode)?.Count ?? 0;
        }

        private int GetStatusCountTotal(IEnumerable<QueryResult.StatusCount> stats)
        {
            return stats.Sum(s => s.Count);
        }
        private string GetRevenueTrendSql(string filterType)
        {
            return filterType?.ToLower() switch
            {
                "today" => @"
                    SELECT
                        DATEADD(HOUR, DATEPART(HOUR, p.PaymentDate),
                            CAST(CAST(p.PaymentDate AS DATE) AS DATETIME)) AS Date,
                        CAST(SUM(p.Amount) AS DECIMAL(18,2)) AS Value
                    FROM Payments p
                    WHERE p.PaymentDate >= @from AND p.PaymentDate < @to
                    AND p.Status = @status AND p.Purpose = @purpose
                    GROUP BY DATEPART(HOUR, p.PaymentDate), CAST(p.PaymentDate AS DATE)
                    ORDER BY Date",

                "year" => @"
                    SELECT
                        DATEFROMPARTS(YEAR(p.PaymentDate), MONTH(p.PaymentDate), 1) AS Date,
                        CAST(SUM(p.Amount) AS DECIMAL(18,2)) AS Value
                    FROM Payments p
                    WHERE p.PaymentDate >= @from AND p.PaymentDate < @to
                    AND p.Status = @status AND p.Purpose = @purpose
                    GROUP BY YEAR(p.PaymentDate), MONTH(p.PaymentDate)
                    ORDER BY Date",

                _ => @"
                    SELECT
                        CAST(CONVERT(VARCHAR(10), p.PaymentDate, 23) AS DATE) AS Date,
                        CAST(SUM(p.Amount) AS DECIMAL(18,2)) AS Value
                    FROM Payments p
                    WHERE p.PaymentDate >= @from AND p.PaymentDate < @to
                    AND p.Status = @status AND p.Purpose = @purpose
                    GROUP BY CAST(CONVERT(VARCHAR(10), p.PaymentDate, 23) AS DATE)
                    ORDER BY Date"
            };
        }
        private string GetOrderTrendSql(string filterType)
        {
            return filterType?.ToLower() switch
            {
                "today" => @"
                    SELECT
                        DATEADD(HOUR, DATEPART(HOUR, o.CreatedDate),
                            CAST(CAST(o.CreatedDate AS DATE) AS DATETIME)) AS Date,
                        COUNT(o.Id) AS Total
                    FROM Orders o
                    WHERE o.CreatedDate >= @from AND o.CreatedDate < @to
                    GROUP BY DATEPART(HOUR, o.CreatedDate), CAST(o.CreatedDate AS DATE)
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
        private string GenerateLabel(DateTime date, string filterType)
        {
            return filterType?.ToLower() switch
            {
                "today" => date.ToString("HH:mm"),
                "week" => new[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" }[(int)date.DayOfWeek],
                "month" => date.Day.ToString("D2"),
                "year" => new[] { "Th1", "Th2", "Th3", "Th4", "Th5", "Th6", "Th7", "Th8", "Th9", "Th10", "Th11", "Th12" }[date.Month - 1],
                _ => string.Empty
            };
        }
    }
}

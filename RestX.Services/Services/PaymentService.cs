using AutoMapper;
using PayOS;
using PayOS.Models.Webhooks;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Payments;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Reservations;
using RestX.BLL.Interfaces.Tables;
using RestX.Models.Customers;
using RestX.Models.Enum;
using RestX.Models.HR;
using RestX.Models.Loyalty;
using RestX.Models.Orders;
using RestX.Models.Reservations;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class PaymentService : BaseService, IPaymentService
    {
        private readonly IPaymentSettingService paymentSettingService;
        private readonly IReservationService reservationService;
        private readonly ITableService tableService;
        private readonly IOrderService orderService;
        private readonly IMapper mapper;

        public PaymentService(
            IRepository repo,
            IRedisService redisService,
            IPaymentSettingService paymentSettingService,
            IReservationService reservationService,
            ITableService tableService,
            IOrderService orderService,
            IMapper mapper,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.paymentSettingService = paymentSettingService;
            this.reservationService = reservationService;
            this.tableService = tableService;
            this.orderService = orderService;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<PaymentDetail>> GetAllPayments(DateTime? from, DateTime? to, string? method, string? statusCode)
        {
            PaymentStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(statusCode) && System.Enum.TryParse<PaymentStatus>(statusCode, true, out var parsed))
                statusFilter = parsed;

            var payments = await Repo.GetAsync<Payment>(
                filter: p =>
                    (from == null || p.PaymentDate >= from) &&
                    (to == null || p.PaymentDate <= to) &&
                    (method == null || p.PaymentMethodId.ToLower() == method.ToLower()) &&
                    (statusFilter == null || p.Status == statusFilter),
                orderBy: q => q.OrderByDescending(p => p.PaymentDate));

            return mapper.Map<IEnumerable<PaymentDetail>>(payments);
        }

        public async Task<IEnumerable<PaymentDetail>> GetPaymentsByOrder(Guid orderId)
        {
            var payments = await Repo.GetAsync<Payment>(
                filter: p => p.OrderId == orderId,
                orderBy: q => q.OrderByDescending(p => p.PaymentDate));

            return mapper.Map<IEnumerable<PaymentDetail>>(payments);
        }

        public async Task<PaymentDetail?> GetPaymentById(Guid id)
        {
            var payment = await Repo.GetOneAsync<Payment>(filter: p => p.Id == id);
            return payment == null ? null : mapper.Map<PaymentDetail>(payment);
        }

        public async Task<CashPaymentResponse> PayByCash(Guid orderId, CashPaymentRequest request, string? createdBy = null)
        {
            Order order = await RecalculateOrderAmountForCheckout(orderId, createdBy);

            var alreadyPaid = await Repo.GetExistsAsync<Payment>(
                p => p.OrderId == orderId && p.Status == PaymentStatus.Success);
            if (alreadyPaid)
                throw new InvalidOperationException("Order is already paid");

            var depositPaid = await GetPaidDepositAmount(order.ReservationId);
            var amountDue = order.TotalAmount - depositPaid;

            if (request.CashReceive < amountDue)
                throw new InvalidOperationException($"Cash received ({request.CashReceive}) is less than amount due ({amountDue})");

            var cashback = request.CashReceive - amountDue;

            var employee = await Repo.GetOneAsync<Employee>(e => e.ApplicationUser.Id.ToString() == createdBy);

            var payment = new Payment
            {
                OrderId = orderId,
                ReservationId = order.ReservationId,
                PaymentMethodId = PaymentConstants.Method.Cash,
                Amount = amountDue,
                CashReceive = request.CashReceive,
                Cashback = cashback,
                Status = PaymentStatus.Success,
                Purpose = PaymentPurpose.Order,
                PaymentDate = DateTime.UtcNow.AddHours(7),
                ProcessedBy = employee != null ? Guid.Parse(employee.Id.ToString()) : null
            };

            await Repo.CreateAsync(payment, createdBy);
            await AwardLoyaltyPointsAsync(order);

            await orderService.UpdateStatus(orderId, (int)OrderStatus.Completed, createdBy ?? string.Empty);

            order.CompletedAt = DateTime.UtcNow.AddHours(7);
            Repo.Update(order, createdBy);

            if (order.ReservationId.HasValue)
            {
                await reservationService.CompleteReservation(order.ReservationId.Value, createdBy);
            }

            List<TableSession> activeSessions = (await Repo.GetAsync<TableSession>(
                filter: ts => ts.OrderId == orderId && ts.IsActive
            )).ToList();

            foreach (Guid tableId in activeSessions.Select(ts => ts.TableId).Distinct())
            {
                await tableService.CloseTableSession(tableId);
            }

            await Repo.SaveAsync();

            return new CashPaymentResponse
            {
                PaymentId = payment.Id,
                Amount = order.TotalAmount,
                CashReceive = request.CashReceive,
                Cashback = cashback
            };
        }

        public async Task<CreatePaymentLinkResponse> CreatePaymentLink(Guid orderId, string? createdBy = null)
        {
            Order order = await RecalculateOrderAmountForCheckout(orderId, createdBy);

            var alreadyPaid = await Repo.GetExistsAsync<Payment>(
                p => p.OrderId == orderId && p.Status == PaymentStatus.Success);
            if (alreadyPaid)
                throw new InvalidOperationException("Order is already paid");

            var depositPaid = await GetPaidDepositAmount(order.ReservationId);
            var amount = (long)(order.TotalAmount - depositPaid);
            if (amount <= 0)
                throw new InvalidOperationException("Order total must be greater than zero");

            var (gatewayClient, gatewaySettings) = await GetTenantGateway();

            var orderCode = GenerateOrderCode();

            var description = $"TT {order.Reference}";
            if (description.Length > 25)
                description = description[..25];

            var items = order.OrderDetails
                .Where(d => !string.Equals(d.ItemStatus?.Code, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                .Select(d => new PayOS.Models.V2.PaymentRequests.PaymentLinkItem
                {
                    Name = d.Dish?.Name ?? "Item",
                    Quantity = d.Quantity,
                    Price = (long)(d.Dish?.Price ?? 0)
                })
                .ToList();

            var linkRequest = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = description,
                Items = items,
                ReturnUrl = $"https://{CurrentTenant.Hostname}/staff/orders?payos=success",
                CancelUrl = $"https://{CurrentTenant.Hostname}/staff/orders?payos=cancel"
            };

            var link = await gatewayClient.PaymentRequests.CreateAsync(linkRequest);
            var employee = await Repo.GetOneAsync<Employee>(e => e.ApplicationUser.Id.ToString() == createdBy);

            var payment = new Payment
            {
                OrderId = orderId,
                ReservationId = order.ReservationId,
                PaymentMethodId = PaymentConstants.Method.Bank,
                Amount = (decimal)amount,
                PayOSOrderCode = orderCode,
                CheckoutUrl = link.CheckoutUrl,
                Status = PaymentStatus.Pending,
                Purpose = PaymentPurpose.Order,
                PaymentDate = DateTime.UtcNow.AddHours(7),
                ProcessedBy = employee != null ? Guid.Parse(employee.Id.ToString()) : null
            };

            await Repo.CreateAsync(payment, createdBy);
            await Repo.SaveAsync();

            return new CreatePaymentLinkResponse
            {
                PaymentId = payment.Id,
                OrderCode = orderCode,
                CheckoutUrl = link.CheckoutUrl
            };
        }
        public async Task CancelPaymentLink(Guid paymentId, string? reason, string? modifiedBy = null)
        {
            var payment = await Repo.GetOneAsync<Payment>(filter: p => p.Id == paymentId)
                ?? throw new KeyNotFoundException("Payment not found");

            if (!string.Equals(payment.PaymentMethodId, PaymentConstants.Method.Bank, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Bank payments can be cancelled");

            if (!payment.PayOSOrderCode.HasValue)
                throw new InvalidOperationException("Payment has no order code");

            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Only PENDING payments can be cancelled");

            var (gatewayClient, _) = await GetTenantGateway();
            await gatewayClient.PaymentRequests.CancelAsync(payment.PayOSOrderCode.Value, reason);

            payment.Status = PaymentStatus.Fail;
            Repo.Update(payment, modifiedBy);
            await Repo.SaveAsync();
        }

        public async Task HandleWebhook(Webhook webhookBody)
        {
            var (gatewayClient, _) = await GetTenantGateway();
            var data = await gatewayClient.Webhooks.VerifyAsync(webhookBody);

            if (webhookBody.Code != "00")
                return;

            var payment = await Repo.GetOneAsync<Payment>(
                filter: p => p.PayOSOrderCode == data.OrderCode)
                ?? throw new KeyNotFoundException($"Payment not found for orderCode {data.OrderCode}");

            if (payment.Status == PaymentStatus.Success)
                return;

            payment.Status = PaymentStatus.Success;
            payment.TransactionId = data.Reference;
            payment.PaymentDate = DateTime.UtcNow.AddHours(7);
            Repo.Update(payment);

            if (payment.Purpose == PaymentPurpose.Deposit && payment.ReservationId.HasValue)
            {
                var reservation = await Repo.GetOneAsync<Reservation>(
                    r => r.Id == payment.ReservationId,
                    includeProperties: "ReservationStatus");
                if (reservation?.ReservationStatus?.Code == "DEPOSIT_PENDING")
                {
                    await reservationService.ConfirmReservation(payment.ReservationId.Value);
                    await reservationService.SendDepositConfirmedEmailAsync(payment.ReservationId.Value);
                }
            }
            else if (payment.Purpose == PaymentPurpose.Order && payment.OrderId.HasValue)
            {
                Order order = await RecalculateOrderAmountForCheckout(payment.OrderId.Value);
                if (order != null)
                {
                    await AwardLoyaltyPointsAsync(order);

                    await orderService.UpdateStatus(order.Id, (int)OrderStatus.Completed, string.Empty);

                    order.CompletedAt = DateTime.UtcNow.AddHours(7);
                    Repo.Update(order);

                    if (order.ReservationId.HasValue)
                    {
                        await reservationService.CompleteReservation(order.ReservationId.Value);
                    }

                    List<TableSession> activeSessions = (await Repo.GetAsync<TableSession>(
                        filter: ts => ts.OrderId == order.Id && ts.IsActive
                    )).ToList();

                    foreach (Guid tableId in activeSessions.Select(ts => ts.TableId).Distinct())
                    {
                        await tableService.CloseTableSession(tableId);
                    }
                }
            }

            await Repo.SaveAsync();
        }

        private async Task<Order> RecalculateOrderAmountForCheckout(Guid orderId, string? modifiedBy = null)
        {
            Order order = await Repo.GetOneAsync<Order>(
                filter: o => o.Id == orderId,
                includeProperties: "OrderDetails.Dish,OrderDetails.ItemStatus")
                ?? throw new KeyNotFoundException("Order not found");

            decimal subTotal = order.OrderDetails
                .Where(d =>
                    d.Dish != null
                    && !string.Equals(d.ItemStatus?.Code, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                .Sum(d => d.Quantity * d.Dish.Price);

            order.SubTotal = subTotal;

            if (order.DiscountAmount > order.SubTotal)
            {
                order.DiscountAmount = order.SubTotal;
            }

            order.CalculateTotalAmount();
            Repo.Update(order, modifiedBy);
            await Repo.SaveAsync();

            return order;
        }

        private async Task<(PayOSClient client, PaymentGatewaySettings settings)> GetTenantGateway()
        {
            var settings = await paymentSettingService.GetPaymentSettingByTenantId(CurrentTenant.Id)
                ?? throw new InvalidOperationException("Payment gateway is not configured for this tenant");
            return (new PayOSClient(settings.ClientId, settings.ApiKey, settings.ChecksumKey, null), settings);
        }

        private async Task AwardLoyaltyPointsAsync(Order order)
        {
            if (!order.CustomerId.HasValue) return;

            var customer = await Repo.GetByIdAsync<Customer>(order.CustomerId.Value);
            if (customer == null) return;

            var points = (int)(order.TotalAmount / 1000);
            if (points <= 0) return;

            customer.LoyaltyPoints += points;

            var bands = await Repo.GetAsync<LoyaltyPointBand>(b => b.IsActive);
            var newBand = bands.FirstOrDefault(b =>
                b.Min <= customer.LoyaltyPoints &&
                (b.Max == null || b.Max >= customer.LoyaltyPoints));
            if (newBand != null && !string.Equals(customer.MembershipLevel, newBand.Name, StringComparison.OrdinalIgnoreCase))
                customer.MembershipLevel = newBand.Name;

            Repo.Update(customer);

            await Repo.CreateAsync(new PointsTransaction
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                OrderId = order.Id,
                Type = "EARN",
                Points = points,
                Description = $"Earned {points} points from order {order.Reference}"
            });
        }

        private async Task<decimal> GetPaidDepositAmount(Guid? reservationId)
        {
            if (!reservationId.HasValue) return 0;
            var deposit = await Repo.GetOneAsync<Payment>(
                p => p.ReservationId == reservationId && p.Purpose == PaymentPurpose.Deposit && p.Status == PaymentStatus.Success);
            return deposit?.Amount ?? 0;
        }

        private static long GenerateOrderCode()
        {
            var timestamp = DateTimeOffset.UtcNow.AddHours(7).ToUnixTimeSeconds();
            var suffix = Random.Shared.Next(100, 999);
            return long.Parse($"{timestamp}{suffix}");
        }
    }
}

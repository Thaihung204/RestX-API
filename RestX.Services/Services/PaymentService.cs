using AutoMapper;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.Webhooks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
        private readonly ILogger<PaymentService> logger;

        public PaymentService(
            IRepository repo,
            IRedisService redisService,
            IPaymentSettingService paymentSettingService,
            IReservationService reservationService,
            ITableService tableService,
            IOrderService orderService,
            IMapper mapper,
            ILogger<PaymentService> logger,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.paymentSettingService = paymentSettingService;
            this.reservationService = reservationService;
            this.tableService = tableService;
            this.orderService = orderService;
            this.mapper = mapper;
            this.logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<IEnumerable<PaymentDetail>> GetAllPayments(DateTime? from, DateTime? to, string? method, string? statusCode, string? purpose)
        {
            PaymentStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(statusCode) && System.Enum.TryParse<PaymentStatus>(statusCode, true, out var parsed))
                statusFilter = parsed;

            PaymentPurpose? purposeFilter = null;
            if (!string.IsNullOrEmpty(purpose) && System.Enum.TryParse<PaymentPurpose>(purpose, true, out var parsedPurpose))
                purposeFilter = parsedPurpose;

            var payments = await Repo.GetAsync<Payment>(
                filter: p =>
                    (from == null || p.PaymentDate >= from) &&
                    (to == null || p.PaymentDate <= to) &&
                    (method == null || p.PaymentMethodId.ToLower() == method.ToLower()) &&
                    (statusFilter == null || p.Status == statusFilter) &&
                    (purposeFilter == null || p.Purpose == purposeFilter),
                orderBy: q => q.OrderByDescending(p => p.PaymentDate),
                includeProperties: "Order.Customer.ApplicationUser,Reservation.Customer.ApplicationUser");

            var result = mapper.Map<IEnumerable<PaymentDetail>>(payments).ToList();
            var paymentList = payments.ToList();
            for (int i = 0; i < result.Count; i++)
            {
                var p = paymentList[i];
                result[i].CustomerName = p.Order?.Customer?.ApplicationUser?.FullName
                    ?? p.Reservation?.Customer?.ApplicationUser?.FullName;
            }
            return result;
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
            var payment = await Repo.GetOneAsync<Payment>(
                filter: p => p.Id == id,
                includeProperties: "Order.Customer.ApplicationUser,Order.OrderDetails.Dish,Order.OrderDetails.ItemStatus,Order.PromotionHistories.Promotion,Reservation.Customer.ApplicationUser");

            if (payment == null) return null;

            var detail = mapper.Map<PaymentDetail>(payment);

            var customer = payment.Order?.Customer ?? payment.Reservation?.Customer;
            if (customer != null)
            {
                detail.CustomerName = customer.ApplicationUser?.FullName;
                detail.Customer = new PaymentCustomerInfo
                {
                    Id = customer.Id,
                    FullName = customer.ApplicationUser?.FullName ?? string.Empty,
                    Phone = customer.ApplicationUser?.PhoneNumber,
                    Email = customer.ApplicationUser?.Email,
                    MembershipLevel = customer.MembershipLevel,
                    LoyaltyPoints = customer.LoyaltyPoints,
                };
            }

            if (payment.Order != null)
            {
                var order = payment.Order;
                detail.Order = new PaymentOrderInfo
                {
                    Id = order.Id,
                    Reference = order.Reference,
                    SubTotal = order.SubTotal,
                    DiscountAmount = order.DiscountAmount,
                    TaxAmount = order.TaxAmount,
                    ServiceCharge = order.ServiceCharge,
                    TotalAmount = order.TotalAmount,
                    Items = order.OrderDetails
                        .Select(d => new PaymentOrderItem
                        {
                            Id = d.Id,
                            DishName = d.Dish?.Name ?? string.Empty,
                            Price = d.Dish?.Price ?? 0,
                            Quantity = d.Quantity,
                            Note = d.Note,
                            ItemStatus = d.ItemStatus?.Code,
                        }).ToList(),
                    Promotions = order.PromotionHistories
                        .Select(ph => new PaymentPromotionInfo
                        {
                            Code = ph.Promotion?.Code ?? string.Empty,
                            Name = ph.Promotion?.Name ?? string.Empty,
                            DiscountAmount = ph.DiscountAmount,
                        }).ToList(),
                };
            }

            if (payment.Reservation != null)
            {
                var res = payment.Reservation;
                detail.Reservation = new PaymentReservationInfo
                {
                    Id = res.Id,
                    ConfirmationCode = res.ConfirmationCode,
                    NumberOfGuests = res.NumberOfGuests,
                    Time = res.Time,
                    SpecialRequests = res.SpecialRequests,
                    DepositAmount = res.DepositAmount,
                };
                detail.DepositPaid = await GetPaidDepositAmount(res.Id);
            }

            return detail;
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

            var existingPending = await Repo.GetOneAsync<Payment>(
                p => p.OrderId == orderId &&
                     p.Purpose == PaymentPurpose.Order &&
                     p.Status == PaymentStatus.Pending);

            Payment payment;
            if (existingPending != null)
            {
                if (existingPending.PayOSOrderCode.HasValue)
                {
                    var (gatewayClient, _) = await GetTenantGateway();
                    await gatewayClient.PaymentRequests.CancelAsync(existingPending.PayOSOrderCode.Value, "Paid by cash");
                }
                existingPending.PaymentMethodId = PaymentConstants.Method.Cash;
                existingPending.Amount = amountDue;
                existingPending.CashReceive = request.CashReceive;
                existingPending.Cashback = cashback;
                existingPending.Status = PaymentStatus.Success;
                existingPending.PaymentDate = DateTime.UtcNow.AddHours(7);
                existingPending.PayOSOrderCode = null;
                existingPending.CheckoutUrl = null;
                existingPending.ProcessedBy = employee?.Id;
                Repo.Update(existingPending, createdBy);
                payment = existingPending;
            }
            else
            {
                payment = new Payment
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
                    ProcessedBy = employee?.Id
                };
                await Repo.CreateAsync(payment, createdBy);
            }
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

            List<Guid> tableIds = activeSessions
                .Select(ts => ts.TableId)
                .Distinct()
                .ToList();

            if (tableIds.Any())
            {
                await tableService.CloseTableSession(tableIds);
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

        public async Task<CreatePaymentLinkResponse> CreatePaymentLink(Guid orderId, string? createdBy = null, bool isCustomer = false)
        {
            Order order = await RecalculateOrderAmountForCheckout(orderId, createdBy);

            var alreadyPaid = await Repo.GetExistsAsync<Payment>(
                p => p.OrderId == orderId && p.Status == PaymentStatus.Success);
            if (alreadyPaid)
                throw new InvalidOperationException("Order is already paid");

            var depositPaid = await GetPaidDepositAmount(order.ReservationId);
            var amount = (long)(order.TotalAmount - depositPaid);
            if (amount <= 0)
                return await ConfirmZeroAmountPayment(order, createdBy);

            var (gatewayClient, gatewaySettings) = await GetTenantGateway();

            var existingPending = await Repo.GetOneAsync<Payment>(
                p => p.OrderId == orderId &&
                     p.Purpose == PaymentPurpose.Order &&
                     p.Status == PaymentStatus.Pending);

            if (existingPending != null)
            {
                var linkAge = DateTime.UtcNow.AddHours(7) - existingPending.PaymentDate;
                if (linkAge.TotalMinutes < 15 && existingPending.CheckoutUrl != null && existingPending.PayOSOrderCode.HasValue)
                {
                    var payosPayment = await gatewayClient.PaymentRequests.GetAsync(existingPending.PayOSOrderCode.Value);
                    if (string.Equals(payosPayment?.Status.ToString(), "PENDING", StringComparison.OrdinalIgnoreCase))
                        return new CreatePaymentLinkResponse
                        {
                            PaymentId = existingPending.Id,
                            OrderCode = existingPending.PayOSOrderCode ?? 0,
                            CheckoutUrl = existingPending.CheckoutUrl
                        };

                    existingPending.Status = PaymentStatus.Fail;
                    Repo.Update(existingPending, createdBy);
                    await Repo.SaveAsync();
                    existingPending = null;
                }
                else if (existingPending.PayOSOrderCode.HasValue)
                {
                    await gatewayClient.PaymentRequests.CancelAsync(existingPending.PayOSOrderCode.Value, "Recreating payment link");
                }
            }

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
                ReturnUrl = isCustomer
                    ? $"https://{CurrentTenant.Hostname}/menu/{order.TableSessions.FirstOrDefault()?.TableId}?payos=success"
                    : $"https://{CurrentTenant.Hostname}/staff/orders?payos=success",
                CancelUrl = isCustomer
                    ? $"https://{CurrentTenant.Hostname}/menu/{order.TableSessions.FirstOrDefault()?.TableId}?payos=cancel"
                    : $"https://{CurrentTenant.Hostname}/staff/orders?payos=cancel"
            };

            var link = await gatewayClient.PaymentRequests.CreateAsync(linkRequest);

            if (existingPending != null)
            {
                existingPending.PayOSOrderCode = orderCode;
                existingPending.CheckoutUrl = link.CheckoutUrl;
                existingPending.Amount = (decimal)amount;
                existingPending.PaymentDate = DateTime.UtcNow.AddHours(7);
                Repo.Update(existingPending, createdBy);
                await Repo.SaveAsync();
                return new CreatePaymentLinkResponse
                {
                    PaymentId = existingPending.Id,
                    OrderCode = orderCode,
                    CheckoutUrl = link.CheckoutUrl
                };
            }

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

        public async Task<WebhookResult> HandleWebhook(Webhook webhookBody)
        {
            logger.LogInformation("[Webhook] Received | Code={Code} | Tenant={Tenant}",
                webhookBody.Code, CurrentTenant?.Hostname ?? "unknown");

            var (gatewayClient, _) = await GetTenantGateway();
            var data = await gatewayClient.Webhooks.VerifyAsync(webhookBody);

            logger.LogInformation("[Webhook] Verified | OrderCode={OrderCode}", data.OrderCode);

            if (webhookBody.Code != "00")
            {
                logger.LogWarning("[Webhook] Non-success code={Code} for OrderCode={OrderCode}", webhookBody.Code, data.OrderCode);
                var cancelledPayment = await Repo.GetOneAsync<Payment>(
                    filter: p => p.PayOSOrderCode == data.OrderCode);
                if (cancelledPayment != null && cancelledPayment.Status == PaymentStatus.Pending)
                {
                    cancelledPayment.Status = PaymentStatus.Fail;
                    Repo.Update(cancelledPayment);
                    await Repo.SaveAsync();
                    logger.LogInformation("[Webhook] Payment {PaymentId} marked Fail", cancelledPayment.Id);
                    return new WebhookResult { Success = false, PaymentId = cancelledPayment.Id, OrderId = cancelledPayment.OrderId, ReservationId = cancelledPayment.ReservationId, Amount = cancelledPayment.Amount, IsDeposit = cancelledPayment.Purpose == PaymentPurpose.Deposit };
                }
                return new WebhookResult { Success = false };
            }

            var payment = await Repo.GetOneAsync<Payment>(
                filter: p => p.PayOSOrderCode == data.OrderCode)
                ?? throw new KeyNotFoundException($"Payment not found for orderCode {data.OrderCode}");

            logger.LogInformation("[Webhook] Payment found | PaymentId={PaymentId} | Purpose={Purpose} | Status={Status}",
                payment.Id, payment.Purpose, payment.Status);

            if (payment.Status == PaymentStatus.Success)
            {
                logger.LogInformation("[Webhook] Payment {PaymentId} already Success, skip", payment.Id);
                return new WebhookResult { Success = true, PaymentId = payment.Id, OrderId = payment.OrderId, ReservationId = payment.ReservationId, Amount = payment.Amount, IsDeposit = payment.Purpose == PaymentPurpose.Deposit };
            }

            payment.Status = PaymentStatus.Success;
            payment.TransactionId = data.Reference;
            payment.PaymentDate = DateTime.UtcNow.AddHours(7);
            Repo.Update(payment);
            await Repo.SaveAsync();

            logger.LogInformation("[Webhook] Payment {PaymentId} saved as Success", payment.Id);

            if (payment.Purpose == PaymentPurpose.Deposit && payment.ReservationId.HasValue)
            {
                logger.LogInformation("[Webhook] Deposit payment for ReservationId={ReservationId}, calling ConfirmReservation",
                    payment.ReservationId.Value);
                try
                {
                    await reservationService.ConfirmReservation(payment.ReservationId.Value);
                    logger.LogInformation("[Webhook] Reservation {ReservationId} confirmed successfully", payment.ReservationId.Value);

                    await reservationService.SendDepositConfirmedEmailAsync(payment.ReservationId.Value);
                    logger.LogInformation("[Webhook] Deposit confirmation email sent for Reservation {ReservationId}", payment.ReservationId.Value);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Webhook] Failed to confirm reservation {ReservationId} after deposit payment {PaymentId}",
                        payment.ReservationId.Value, payment.Id);
                    throw;
                }
            }
            else if (payment.Purpose == PaymentPurpose.Order && payment.OrderId.HasValue)
            {
                logger.LogInformation("[Webhook] Order payment for OrderId={OrderId}", payment.OrderId.Value);
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

                    List<Guid> tableIds = activeSessions
                        .Select(ts => ts.TableId)
                        .Distinct()
                        .ToList();

                    if (tableIds.Any())
                    {
                        await tableService.CloseTableSession(tableIds);
                    }
                }
            }

            await Repo.SaveAsync();
            logger.LogInformation("[Webhook] HandleWebhook completed for OrderCode={OrderCode}", data.OrderCode);

            return new WebhookResult
            {
                Success = true,
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                ReservationId = payment.ReservationId,
                Amount = payment.Amount,
                IsDeposit = payment.Purpose == PaymentPurpose.Deposit
            };
        }

        private async Task<CreatePaymentLinkResponse> ConfirmZeroAmountPayment(Order order, string? createdBy)
        {
            var employee = await Repo.GetOneAsync<Employee>(e => e.ApplicationUser.Id.ToString() == createdBy);

            var existingPending = await Repo.GetOneAsync<Payment>(
                p => p.OrderId == order.Id &&
                     p.Purpose == PaymentPurpose.Order &&
                     p.Status == PaymentStatus.Pending);

            Payment payment;
            if (existingPending != null)
            {
                existingPending.PaymentMethodId = PaymentConstants.Method.Cash;
                existingPending.Amount = 0;
                existingPending.Status = PaymentStatus.Success;
                existingPending.PaymentDate = DateTime.UtcNow.AddHours(7);
                existingPending.PayOSOrderCode = null;
                existingPending.CheckoutUrl = null;
                existingPending.ProcessedBy = employee?.Id;
                Repo.Update(existingPending, createdBy);
                payment = existingPending;
            }
            else
            {
                payment = new Payment
                {
                    OrderId = order.Id,
                    ReservationId = order.ReservationId,
                    PaymentMethodId = PaymentConstants.Method.Cash,
                    Amount = 0,
                    Status = PaymentStatus.Success,
                    Purpose = PaymentPurpose.Order,
                    PaymentDate = DateTime.UtcNow.AddHours(7),
                    ProcessedBy = employee?.Id
                };
                await Repo.CreateAsync(payment, createdBy);
            }

            await AwardLoyaltyPointsAsync(order);
            await orderService.UpdateStatus(order.Id, (int)OrderStatus.Completed, createdBy ?? string.Empty);

            order.CompletedAt = DateTime.UtcNow.AddHours(7);
            Repo.Update(order, createdBy);

            if (order.ReservationId.HasValue)
                await reservationService.CompleteReservation(order.ReservationId.Value, createdBy);

            List<TableSession> activeSessions = (await Repo.GetAsync<TableSession>(
                filter: ts => ts.OrderId == order.Id && ts.IsActive
            )).ToList();

            List<Guid> tableIds = activeSessions
                .Select(ts => ts.TableId)
                .Distinct()
                .ToList();

            if (tableIds.Any())
            {
                await tableService.CloseTableSession(tableIds);
            }

            await Repo.SaveAsync();

            return new CreatePaymentLinkResponse
            {
                PaymentId = payment.Id,
                OrderCode = 0,
                CheckoutUrl = string.Empty,
                ZeroAmount = true
            };
        }

        public async Task<Order> RecalculateOrderAmountForCheckout(Guid orderId, string? modifiedBy = null)
        {
            Order order = await Repo.GetOneAsync<Order>(
                filter: o => o.Id == orderId,
                includeProperties: "OrderDetails.Dish,OrderDetails.ItemStatus,OrderDetails.Combo,TableSessions")
                ?? throw new KeyNotFoundException("Order not found");

            decimal subTotal = order.OrderDetails
                .Where(d =>
                    d.ParentId == null
                    && !string.Equals(d.ItemStatus?.Code, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                .Sum(d =>
                    d.ComboId.HasValue
                        ? (d.Combo?.Price ?? d.UnitPrice) * d.Quantity
                        : (d.Dish?.Price ?? d.UnitPrice) * d.Quantity);

            order.SubTotal = subTotal;

            if (order.DiscountAmount > order.SubTotal)
            {
                order.DiscountAmount = order.SubTotal;
            }

            var afterDiscount = order.SubTotal - order.DiscountAmount;
            order.TaxAmount = Math.Round(afterDiscount * CurrentTenant.TaxRate / 100, 2);
            order.ServiceCharge = Math.Round(afterDiscount * CurrentTenant.ServiceChargeRate / 100, 2);

            order.CalculateTotalAmount();
            if (order.ReservationId.HasValue)
            {
                Models.Reservations.Reservation? reservation =
                    await Repo.GetByIdAsync<Models.Reservations.Reservation>(order.ReservationId.Value);

                if (reservation != null)
                {
                    order.ApplyReservationDeposit(reservation.DepositAmount);
                }
            }
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

        #region Receipt

        public async Task<byte[]> GenerateReceiptAsync(Guid paymentId)
        {
            var payment = await Repo.GetOneAsync<Payment>(
                filter: p => p.Id == paymentId,
                includeProperties: "Reservation")
                ?? throw new KeyNotFoundException("Payment not found");

            var tenant = CurrentTenant;

            if (payment.Purpose == PaymentPurpose.Deposit && payment.ReservationId.HasValue)
                return await GenerateDepositReceiptAsync(payment, tenant);

            if (!payment.OrderId.HasValue)
                throw new InvalidOperationException("Payment has no associated order");

            var order = await Repo.GetOneAsync<Order>(
                filter: o => o.Id == payment.OrderId,
                includeProperties: "OrderDetails.Dish,OrderDetails.ItemStatus,PromotionHistories.Promotion,Customer.ApplicationUser")
                ?? throw new KeyNotFoundException("Order not found");

            decimal depositPaid = await GetPaidDepositAmount(order.ReservationId);

            var promotionDiscount = order.PromotionHistories?.Sum(ph => ph.DiscountAmount) ?? 0;
            decimal membershipDiscount = 0;
            if (order.DiscountAmount > promotionDiscount)
                membershipDiscount = order.DiscountAmount - promotionDiscount;

            var activeItems = order.OrderDetails
                .Where(d => !string.Equals(d.ItemStatus?.Code, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        col.Item().AlignCenter().Text(tenant?.BusinessName ?? tenant?.Name ?? "Restaurant")
                            .FontSize(18).Bold();

                        if (!string.IsNullOrEmpty(tenant?.BusinessAddressLine1))
                            col.Item().AlignCenter().Text(BuildAddress(tenant)).FontSize(8);

                        if (!string.IsNullOrEmpty(tenant?.BusinessPrimaryPhone))
                            col.Item().AlignCenter().Text($"Tel: {tenant.BusinessPrimaryPhone}").FontSize(8);

                        if (!string.IsNullOrEmpty(tenant?.BusinessEmailAddress))
                            col.Item().AlignCenter().Text(tenant.BusinessEmailAddress).FontSize(8);

                        col.Item().PaddingVertical(6).LineHorizontal(1f);

                        col.Item().AlignCenter().Text("HÓA ĐƠN THANH TOÁN").FontSize(14).Bold();

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Số HĐ: {order.Reference}").Bold();
                                if (order.Customer?.ApplicationUser != null)
                                    c.Item().Text($"Khách hàng: {order.Customer.ApplicationUser.FullName}");
                                if (!string.IsNullOrEmpty(order.Customer?.ApplicationUser?.PhoneNumber))
                                    c.Item().Text($"SĐT: {order.Customer.ApplicationUser.PhoneNumber}");
                                if (!string.IsNullOrEmpty(order.Customer?.MembershipLevel))
                                    c.Item().Text($"Hạng thành viên: {order.Customer.MembershipLevel}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text($"Ngày: {payment.PaymentDate:dd/MM/yyyy}");
                                c.Item().AlignRight().Text($"Giờ: {payment.PaymentDate:HH:mm}");
                                c.Item().AlignRight().Text($"Mã GD: {payment.TransactionId ?? payment.Id.ToString()[..8].ToUpper()}").FontSize(8);
                            });
                        });

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        col.Item().Background("#f0f0f0").Padding(3).Row(row =>
                        {
                            row.RelativeItem(5).Text("Món").Bold();
                            row.RelativeItem(1).AlignCenter().Text("SL").Bold();
                            row.RelativeItem(2).AlignRight().Text("Đơn giá").Bold();
                            row.RelativeItem(2).AlignRight().Text("Thành tiền").Bold();
                        });

                        col.Item().PaddingBottom(3).LineHorizontal(0.5f);

                        foreach (var item in activeItems)
                        {
                            var unitPrice = item.Dish?.Price ?? 0;
                            col.Item().PaddingVertical(2).Row(row =>
                            {
                                row.RelativeItem(5).Text(item.Dish?.Name ?? "Item");
                                row.RelativeItem(1).AlignCenter().Text(item.Quantity.ToString());
                                row.RelativeItem(2).AlignRight().Text(FormatCurrency(unitPrice));
                                row.RelativeItem(2).AlignRight().Text(FormatCurrency(unitPrice * item.Quantity));
                            });
                        }

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Tạm tính");
                            row.RelativeItem().AlignRight().Text(FormatCurrency(order.SubTotal));
                        });

                        if (promotionDiscount > 0)
                        {
                            foreach (var ph in order.PromotionHistories.Where(ph => ph.DiscountAmount > 0))
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Khuyến mãi ({ph.Promotion?.Code ?? "KM"})").FontColor("#c0392b");
                                    row.RelativeItem().AlignRight().Text($"- {FormatCurrency(ph.DiscountAmount)}").FontColor("#c0392b");
                                });
                            }
                        }

                        if (membershipDiscount > 0)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Ưu đãi thành viên ({order.Customer?.MembershipLevel})").FontColor("#c0392b");
                                row.RelativeItem().AlignRight().Text($"- {FormatCurrency(membershipDiscount)}").FontColor("#c0392b");
                            });
                        }

                        if (order.TaxAmount > 0)
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Thuế VAT");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(order.TaxAmount));
                            });

                        if (order.ServiceCharge > 0)
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Phí dịch vụ");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(order.ServiceCharge));
                            });

                        col.Item().PaddingVertical(4).LineHorizontal(1f);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("TỔNG CỘNG").Bold().FontSize(12);
                            row.RelativeItem().AlignRight().Text(FormatCurrency(order.TotalAmount)).Bold().FontSize(12);
                        });

                        if (depositPaid > 0)
                        {
                            col.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem().Text("Đã đặt cọc").FontColor("#27ae60");
                                row.RelativeItem().AlignRight().Text($"- {FormatCurrency(depositPaid)}").FontColor("#27ae60");
                            });
                            col.Item().PaddingVertical(3).LineHorizontal(0.5f);
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("SỐ TIỀN THANH TOÁN").Bold().FontSize(12);
                                row.RelativeItem().AlignRight().Text(FormatCurrency(order.TotalAmount - depositPaid)).Bold().FontSize(12);
                            });
                        }

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        col.Item().Text("Thông tin thanh toán").Bold();
                        col.Item().PaddingTop(2).Row(row =>
                        {
                            row.RelativeItem().Text("Phương thức");
                            row.RelativeItem().AlignRight().Text(
                                string.Equals(payment.PaymentMethodId, PaymentConstants.Method.Cash, StringComparison.OrdinalIgnoreCase)
                                    ? "Tiền mặt" : "Chuyển khoản");
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Trạng thái");
                            row.RelativeItem().AlignRight().Text(
                                payment.Status == PaymentStatus.Success ? "Đã thanh toán" : payment.Status.ToString())
                                .FontColor(payment.Status == PaymentStatus.Success ? "#27ae60" : "#e74c3c");
                        });

                        if (string.Equals(payment.PaymentMethodId, PaymentConstants.Method.Cash, StringComparison.OrdinalIgnoreCase) && payment.CashReceive > 0)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Tiền khách đưa");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(payment.CashReceive));
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Tiền thối lại");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(payment.Cashback));
                            });
                        }

                        if (!string.IsNullOrEmpty(payment.TransactionId))
                            col.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Mã giao dịch");
                                row.RelativeItem().AlignRight().Text(payment.TransactionId).FontSize(8);
                            });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f);

                        col.Item().AlignCenter().Text("Cảm ơn quý khách! Hẹn gặp lại.").Italic().FontSize(9);

                        if (!string.IsNullOrEmpty(tenant?.BusinessOpeningHours))
                            col.Item().AlignCenter().Text($"Giờ mở cửa: {tenant.BusinessOpeningHours}").FontSize(8);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private async Task<byte[]> GenerateDepositReceiptAsync(Payment payment, ActiveTenant tenant)
        {
            var reservation = await Repo.GetOneAsync<Reservation>(
                filter: r => r.Id == payment.ReservationId,
                includeProperties: "Customer.ApplicationUser,ReservationStatus")
                ?? throw new KeyNotFoundException("Reservation not found");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        col.Item().AlignCenter().Text(tenant?.BusinessName ?? tenant?.Name ?? "Restaurant")
                            .FontSize(18).Bold();

                        if (!string.IsNullOrEmpty(tenant?.BusinessAddressLine1))
                            col.Item().AlignCenter().Text(BuildAddress(tenant)).FontSize(8);

                        if (!string.IsNullOrEmpty(tenant?.BusinessPrimaryPhone))
                            col.Item().AlignCenter().Text($"Tel: {tenant.BusinessPrimaryPhone}").FontSize(8);

                        col.Item().PaddingVertical(6).LineHorizontal(1f);

                        col.Item().AlignCenter().Text("BIÊN LAI ĐẶT CỌC").FontSize(14).Bold();

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                if (reservation.ConfirmationCode != null)
                                    c.Item().Text($"Mã xác nhận: {reservation.ConfirmationCode}").Bold();
                                c.Item().Text($"Khách hàng: {reservation.Customer?.ApplicationUser?.FullName ?? "N/A"}");
                                if (!string.IsNullOrEmpty(reservation.Customer?.ApplicationUser?.PhoneNumber))
                                    c.Item().Text($"SĐT: {reservation.Customer.ApplicationUser.PhoneNumber}");
                                c.Item().Text($"Số khách: {reservation.NumberOfGuests}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text($"Ngày: {payment.PaymentDate:dd/MM/yyyy HH:mm}");
                                c.Item().AlignRight().Text($"Giờ đặt bàn: {reservation.Time:dd/MM/yyyy HH:mm}");
                            });
                        });

                        if (!string.IsNullOrEmpty(reservation.SpecialRequests))
                            col.Item().PaddingTop(4).Text($"Yêu cầu đặc biệt: {reservation.SpecialRequests}").Italic().FontSize(8);

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Yêu cầu đặt cọc").Bold();
                            row.RelativeItem().AlignRight().Text(FormatCurrency(reservation.DepositAmount)).Bold();
                        });

                        col.Item().PaddingVertical(4).LineHorizontal(1f);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("SỐ TIỀN ĐÃ CỌC").Bold().FontSize(12);
                            row.RelativeItem().AlignRight().Text(FormatCurrency(payment.Amount)).Bold().FontSize(12).FontColor("#27ae60");
                        });

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        col.Item().Text("Thông tin thanh toán").Bold();
                        col.Item().PaddingTop(2).Row(row =>
                        {
                            row.RelativeItem().Text("Phương thức");
                            row.RelativeItem().AlignRight().Text(
                                string.Equals(payment.PaymentMethodId, PaymentConstants.Method.Cash, StringComparison.OrdinalIgnoreCase)
                                    ? "Tiền mặt" : "Chuyển khoản");
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Trạng thái");
                            row.RelativeItem().AlignRight().Text("Đã xác nhận").FontColor("#27ae60");
                        });

                        if (!string.IsNullOrEmpty(payment.TransactionId))
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Mã giao dịch");
                                row.RelativeItem().AlignRight().Text(payment.TransactionId).FontSize(8);
                            });

                        col.Item().PaddingTop(6).Text("* Tiền cọc sẽ được trừ vào hóa đơn cuối khi thanh toán.").FontSize(8).Italic();

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f);

                        col.Item().AlignCenter().Text("Cảm ơn quý khách! Hẹn gặp lại.").Italic().FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string BuildAddress(ActiveTenant tenant)
        {
            var parts = new[] { tenant.BusinessAddressLine1, tenant.BusinessAddressLine2, tenant.BusinessAddressLine3 }
                .Where(s => !string.IsNullOrEmpty(s));
            return string.Join(", ", parts);
        }

        private static string FormatCurrency(decimal amount) =>
            string.Format("{0:N0} VND", amount);

        #endregion
    }
}

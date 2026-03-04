using AutoMapper;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.Webhooks;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Payments;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Status;
using RestX.Models.Enum;
using RestX.Models.Orders;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class PaymentService : BaseService, IPaymentService
    {
        private readonly PayOSClient payOS;
        private readonly PayOSSettings settings;
        private readonly IStatusValueService statusValueService;
        private readonly IMapper mapper;

        private const string PaymentStatusType = "PAYMENT";

        public PaymentService(
            IRepository repo,
            IRedisService redisService,
            IOptions<PayOSSettings> payOSOptions,
            IStatusValueService statusValueService,
            IMapper mapper,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            settings = payOSOptions.Value;
            payOS = new PayOSClient(settings.ClientId, settings.ApiKey, settings.ChecksumKey, null);
            this.statusValueService = statusValueService;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<PaymentDetail>> GetAllPayments(DateTime? from, DateTime? to, string? method, string? statusCode)
        {
            int? statusId = null;
            if (!string.IsNullOrEmpty(statusCode))
            {
                var statuses = await statusValueService.GetStatuses(PaymentStatusType);
                statusId = statuses.FirstOrDefault(s => s.Code == statusCode)?.Id;
            }

            var payments = await Repo.GetAsync<Payment>(
                filter: p =>
                    (from == null || p.PaymentDate >= from) &&
                    (to == null || p.PaymentDate <= to) &&
                    (method == null || p.PaymentMethodId == method) &&
                    (statusId == null || p.PaymentStatusId == statusId),
                includeProperties: "PaymentStatus",
                orderBy: q => q.OrderByDescending(p => p.PaymentDate));

            return mapper.Map<IEnumerable<PaymentDetail>>(payments);
        }

        public async Task<IEnumerable<PaymentDetail>> GetPaymentsByOrder(Guid orderId)
        {
            var payments = await Repo.GetAsync<Payment>(
                filter: p => p.OrderId == orderId,
                includeProperties: "PaymentStatus",
                orderBy: q => q.OrderByDescending(p => p.PaymentDate));

            return mapper.Map<IEnumerable<PaymentDetail>>(payments);
        }

        public async Task<PaymentDetail?> GetPaymentById(Guid id)
        {
            var payment = await Repo.GetOneAsync<Payment>(
                filter: p => p.Id == id,
                includeProperties: "PaymentStatus");

            return payment == null ? null : mapper.Map<PaymentDetail>(payment);
        }

        public async Task<CashPaymentResponse> PayByCash(Guid orderId, CashPaymentRequest request)
        {
            var order = await Repo.GetOneAsync<Order>(filter: o => o.Id == orderId)
                ?? throw new KeyNotFoundException("Order not found");

            if (order.PaymentStatusId == PaymentStatus.Paid)
                throw new InvalidOperationException("Order is already paid");

            if (request.CashReceive < order.TotalAmount)
                throw new InvalidOperationException($"Cash received ({request.CashReceive}) is less than order total ({order.TotalAmount})");

            var cashback = request.CashReceive - order.TotalAmount;
            var paidStatusId = await FindStatus("PAID");

            var payment = new Payment
            {
                OrderId = orderId,
                PaymentMethodId = "CASH",
                Amount = order.TotalAmount,
                CashReceive = request.CashReceive,
                Cashback = cashback,
                PaymentStatusId = paidStatusId,
                PaymentDate = DateTime.UtcNow
            };

            await Repo.CreateAsync(payment);

            order.PaymentStatusId = PaymentStatus.Paid;
            Repo.Update(order);

            await Repo.SaveAsync();

            return new CashPaymentResponse
            {
                PaymentId = payment.Id,
                Amount = order.TotalAmount,
                CashReceive = request.CashReceive,
                Cashback = cashback
            };
        }

        public async Task<CreatePaymentLinkResponse> CreatePayOSLink(Guid orderId)
        {
            var order = await Repo.GetOneAsync<Order>(
                filter: o => o.Id == orderId,
                includeProperties: "OrderDetails.Dish")
                ?? throw new KeyNotFoundException("Order not found");

            if (order.PaymentStatusId == PaymentStatus.Paid)
                throw new InvalidOperationException("Order is already paid");

            var amount = (long)order.TotalAmount;
            if (amount <= 0)
                throw new InvalidOperationException("Order total must be greater than zero");

            var orderCode = GenerateOrderCode();

            var description = $"TT {order.Reference}";
            if (description.Length > 25)
                description = description[..25];

            var items = order.OrderDetails.Select(d =>
                new PayOS.Models.V2.PaymentRequests.PaymentLinkItem { Name = d.Dish?.Name ?? "Item", Quantity = d.Quantity, Price = (long)(d.Dish?.Price ?? 0) }
            ).ToList();

            var request = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = description,
                Items = items,
                ReturnUrl = settings.ReturnUrl,
                CancelUrl = settings.CancelUrl
            };

            var link = await payOS.PaymentRequests.CreateAsync(request);

            var pendingStatus = await FindStatus("UNPAID");

            var payment = new Payment
            {
                OrderId = orderId,
                PaymentMethodId = "Bank",
                Amount = order.TotalAmount,
                PayOSOrderCode = orderCode,
                CheckoutUrl = link.CheckoutUrl,
                PaymentStatusId = pendingStatus,
                PaymentDate = DateTime.UtcNow
            };

            await Repo.CreateAsync(payment);
            await Repo.SaveAsync();

            return new CreatePaymentLinkResponse
            {
                PaymentId = payment.Id,
                OrderCode = orderCode,
                CheckoutUrl = link.CheckoutUrl
            };
        }

        public async Task CancelPayOSLink(Guid paymentId, string? reason)
        {
            var payment = await Repo.GetOneAsync<Payment>(filter: p => p.Id == paymentId)
                ?? throw new KeyNotFoundException("Payment not found");

            if (payment.PaymentMethodId != "Bank")
                throw new InvalidOperationException("Only Bank payments can be cancelled");

            if (!payment.PayOSOrderCode.HasValue)
                throw new InvalidOperationException("Payment has no PayOS order code");

            var cancelledStatusId = await FindStatus("CANCELLED");
            var currentStatus = await FindStatus("UNPAID");

            if (payment.PaymentStatusId != currentStatus)
                throw new InvalidOperationException("Only UNPAID payments can be cancelled");

            await payOS.PaymentRequests.CancelAsync(payment.PayOSOrderCode.Value, reason);

            payment.PaymentStatusId = cancelledStatusId;
            Repo.Update(payment);
            await Repo.SaveAsync();
        }

        public async Task HandleWebhook(Webhook webhookBody)
        {
            var data = await payOS.Webhooks.VerifyAsync(webhookBody);

            if (webhookBody.Code != "00")
                return;

            var payment = await Repo.GetOneAsync<Payment>(
                filter: p => p.PayOSOrderCode == data.OrderCode)
                ?? throw new KeyNotFoundException($"Payment not found for orderCode {data.OrderCode}");

            payment.PaymentStatusId = await FindStatus("PAID");
            payment.TransactionId = data.Reference;
            Repo.Update(payment);

            if (payment.OrderId.HasValue)
            {
                var order = await Repo.GetByIdAsync<Order>(payment.OrderId.Value);
                if (order != null)
                {
                    order.PaymentStatusId = PaymentStatus.Paid;
                    Repo.Update(order);
                }
            }

            await Repo.SaveAsync();
        }

        private async Task<int> FindStatus(string code)
        {
            var statuses = await statusValueService.GetStatuses(PaymentStatusType);
            var status = statuses.FirstOrDefault(s => s.Code == code)
                ?? throw new InvalidOperationException($"{PaymentStatusType}/{code} not found");
            return status.Id;
        }

        private static long GenerateOrderCode()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var suffix = Random.Shared.Next(100, 999);
            return long.Parse($"{timestamp}{suffix}");
        }
    }
}

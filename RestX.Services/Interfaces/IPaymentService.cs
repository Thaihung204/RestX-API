using PayOS.Models.Webhooks;
using RestX.BLL.DataTranferObjects.Payments;

namespace RestX.BLL.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDetail>> GetAllPayments(DateTime? from, DateTime? to, string? method, string? statusCode);
        Task<IEnumerable<PaymentDetail>> GetPaymentsByOrder(Guid orderId);
        Task<PaymentDetail?> GetPaymentById(Guid id);
        Task<CashPaymentResponse> PayByCash(Guid orderId, CashPaymentRequest request);
        Task<CreatePaymentLinkResponse> CreatePayOSLink(Guid orderId);
        Task CancelPayOSLink(Guid paymentId, string? reason);
        Task HandleWebhook(Webhook webhookBody);
    }
}

using PayOS.Models.Webhooks;
using RestX.BLL.DataTranferObjects.Payments;
using System.Threading.Tasks;

namespace RestX.BLL.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDetail>> GetAllPayments(DateTime? from, DateTime? to, string? method, string? statusCode, string? purpose);
        Task<IEnumerable<PaymentDetail>> GetPaymentsByOrder(Guid orderId);
        Task<PaymentDetail?> GetPaymentById(Guid id);
        Task<CashPaymentResponse> PayByCash(Guid orderId, CashPaymentRequest request, string? createdBy = null);
        Task<CreatePaymentLinkResponse> CreatePaymentLink(Guid orderId, string? createdBy = null);
        Task CancelPaymentLink(Guid paymentId, string? reason, string? modifiedBy = null);
        Task<bool> HandleWebhook(Webhook webhookBody);
        Task<byte[]> GenerateReceiptAsync(Guid paymentId);
    }
}

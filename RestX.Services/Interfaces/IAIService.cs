using Microsoft.AspNetCore.Http;
using RestX.BLL.DataTranferObjects.AI;

namespace RestX.BLL.Interfaces
{
    public interface IAIService
    {
        Task<string> ResolveSession(string? cookieSessionId, string? userId);
        Task<AIChatResponse> Chat(AIChatRequest request);
        Task ChatStream(AIChatRequest request, HttpResponse httpResponse);
        Task<Guid> ConfirmOrder(string sessionId, string userId, AIOrderDraft draft);
        Task ClearSession(string sessionId);
        Task<ChatHistoryResponse?> GetHistory(string sessionId);
        Task CleanupExpiredSessions();
    }
}

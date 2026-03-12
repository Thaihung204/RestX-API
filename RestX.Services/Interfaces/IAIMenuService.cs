using Microsoft.AspNetCore.Http;
using RestX.BLL.DataTranferObjects.AI;

namespace RestX.BLL.Interfaces
{
    public interface IAIMenuService
    {
        Task<AIChatResponse> ChatAsync(AIChatRequest request);
        Task ChatStreamAsync(AIChatRequest request, HttpResponse httpResponse);
        Task ClearSessionAsync(string sessionId);
    }
}

using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Feedback;

namespace RestX.BLL.Interfaces.Feedbacks
{
    public interface IFeedbackService
    {
        Task<FeedbackItem> CreateFeedback(Guid orderId, Guid customerId, FeedbackCreate request);
        Task<FeedbackItem?> GetFeedbackById(Guid id, Guid? callerCustomerId = null, bool isAdmin = false);
        Task<FeedbackItem> UpdateFeedback(Guid id, Guid callerCustomerId, FeedbackUpdate request, bool isAdmin = false);
        Task DeleteFeedback(Guid id, Guid callerCustomerId, bool isAdmin = false);
        Task<PaginatedResult<FeedbackItem>> GetAdminFeedbacks(FeedbackFilterParams filter);
    }
}
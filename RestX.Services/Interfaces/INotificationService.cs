using RestX.BLL.DataTranferObjects;

namespace RestX.BLL.Interfaces
{
    public interface INotificationService
    {
        Task<List<RestaurantNotification>> GetAllNotifications();
        Task<List<RestaurantNotification>> GetNotificationByRecipentId(string? recipentId);
        Task<RestaurantNotification?> GetNotificationById(Guid id);
        Task<RestaurantNotification> CreateNotification(RestaurantNotification model, string userId);
        Task<Guid> UpdateNotification(Guid id, RestaurantNotification model, string userId);
        Task<bool> DeleteNotification(Guid id);
        Task<bool> SetPublishStatus(Guid id, bool isPublished, string userId);
    }
}
using AutoMapper;
using RestX.BLL.DataTranferObjects;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Enum;
using RestX.Models.Notifications;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class NotificationService : BaseService, INotificationService
    {
        private readonly IMapper mapper;

        public NotificationService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }

        public async Task<List<RestaurantNotification>> GetAllNotifications()
        {
            IEnumerable<Notification> entities = await Repo.GetAllAsync<Notification>(
                orderBy: q => q.OrderByDescending(x => x.CreatedDate)
            );

            return mapper.Map<List<RestaurantNotification>>(entities.ToList());
        }

        public async Task<List<RestaurantNotification>> GetNotificationByRecipentId(string? recipentId)
        {
            DateTime now = DateTime.UtcNow.AddHours(7);
            IEnumerable<Notification> entities;

            if (string.IsNullOrWhiteSpace(recipentId))
            {
                entities = await Repo.GetAsync<Notification>(
                    filter: x =>
                        x.IsPublished
                        && (x.ExpiryDate == null || x.ExpiryDate >= now)
                        && x.IsBroadcast,
                    orderBy: q => q.OrderByDescending(x => x.CreatedDate)
                );
            }
            else
            {
                string recipient = recipentId.Trim();
                entities = await Repo.GetAsync<Notification>(
                    filter: x =>
                        x.IsPublished
                        && (x.ExpiryDate == null || x.ExpiryDate >= now)
                        && (x.IsBroadcast || x.RecipientId == recipient),
                    orderBy: q => q.OrderByDescending(x => x.CreatedDate)
                );
            }

            return mapper.Map<List<RestaurantNotification>>(entities.ToList());
        }

        public async Task<RestaurantNotification?> GetNotificationById(Guid id)
        {
            Notification entity = await Repo.GetByIdAsync<Notification>(id);
            if (entity == null)
            {
                return null;
            }

            return mapper.Map<RestaurantNotification>(entity);
        }

        public async Task<RestaurantNotification> CreateNotification(RestaurantNotification model, string userId)
        {
            NormalizeModel(model);
            ValidateBusinessRules(model);

            Notification entity = mapper.Map<Notification>(model);

            await Repo.CreateAsync(entity, userId);
            await Repo.SaveAsync();

            return mapper.Map<RestaurantNotification>(entity);
        }

        public async Task<Guid> UpdateNotification(Guid id, RestaurantNotification model, string userId)
        {
            NormalizeModel(model);
            ValidateBusinessRules(model);

            Notification entity = await Repo.GetByIdAsync<Notification>(id);
            if (entity == null)
            {
                return Guid.Empty;
            }

            mapper.Map(model, entity);

            Repo.Update(entity, userId);
            await Repo.SaveAsync();

            return entity.Id;
        }

        public async Task<bool> DeleteNotification(Guid id)
        {
            Notification entity = await Repo.GetByIdAsync<Notification>(id);
            if (entity == null)
            {
                return false;
            }

            Repo.Delete<Notification>(id);
            await Repo.SaveAsync();

            return true;
        }

        public async Task<bool> SetPublishStatus(Guid id, bool isPublished, string userId)
        {
            Notification entity = await Repo.GetByIdAsync<Notification>(id);
            if (entity == null)
            {
                return false;
            }

            entity.IsPublished = isPublished;
            Repo.Update(entity, userId);
            await Repo.SaveAsync();

            return true;
        }

        private static void NormalizeModel(RestaurantNotification model)
        {
            model.Title = model.Title?.Trim() ?? string.Empty;
            model.Message = model.Message?.Trim() ?? string.Empty;
            model.NotificationType = model.NotificationType?.Trim().ToUpperInvariant() ?? string.Empty;
            model.Priority = model.Priority?.Trim().ToUpperInvariant() ?? string.Empty;

            if (model.IsBroadcast)
            {
                model.RecipientId = null;
            }
            else
            {
                model.RecipientId = model.RecipientId?.Trim();
            }
        }

        private static void ValidateBusinessRules(RestaurantNotification model)
        {
            if (!model.IsBroadcast && string.IsNullOrWhiteSpace(model.RecipientId))
            {
                throw new AppException("RecipientId is required for non-broadcast notification");
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                throw new AppException("Title is required");
            }

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                throw new AppException("Message is required");
            }

            bool isValidType = Enum.TryParse<NotificationType>(model.NotificationType, true, out _)
                               && !int.TryParse(model.NotificationType, out _);
            if (!isValidType)
            {
                throw new AppException("Invalid NotificationType");
            }

            bool isValidPriority = Enum.TryParse<NotificationPriority>(model.Priority, true, out _)
                                   && !int.TryParse(model.Priority, out _);
            if (!isValidPriority)
            {
                throw new AppException("Invalid Priority");
            }
        }
    }
}
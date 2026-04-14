using AutoMapper;
using RestX.BLL.DataTranferObjects;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Notifications;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class NotificationService : BaseService, INotificationService
    {
        private readonly IMapper mapper;

        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "INFO", "ORDER", "RESERVATION", "PAYMENT", "PROMOTION", "SYSTEM"
        };

        private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
        {
            "LOW", "NORMAL", "HIGH", "URGENT"
        };

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

        public async Task<List<RestaurantNotification>> GetMyNotifications(string? recipientId)
        {
            DateTime now = DateTime.UtcNow.AddHours(7);
            IEnumerable<Notification> entities;

            if (string.IsNullOrWhiteSpace(recipientId))
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
                string recipient = recipientId.Trim();
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

            if (!AllowedTypes.Contains(model.NotificationType))
            {
                throw new AppException("Invalid NotificationType");
            }

            if (!AllowedPriorities.Contains(model.Priority))
            {
                throw new AppException("Invalid Priority");
            }
        }
    }
}
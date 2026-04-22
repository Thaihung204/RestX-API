using AutoMapper;
using Microsoft.AspNetCore.Identity;
using RestX.BLL.DataTranferObjects;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.Models.Enum;
using RestX.Models.Identity;
using RestX.Models.Notifications;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class NotificationService : BaseService, INotificationService
    {
        private readonly IMapper mapper;
        private readonly IOrderService orderService;
        private readonly UserManager<ApplicationUser> userManager;

        public NotificationService(
            IMapper mapper,
            IOrderService orderService,
            UserManager<ApplicationUser> userManager,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
            this.orderService = orderService;
            this.userManager = userManager;
        }

        public async Task<List<RestaurantNotification>> CreateRequestByTableId(Guid tableId, string title, string userId)
        {
            if (tableId == Guid.Empty)
            {
                throw new AppException("TableId is required");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new AppException("Title is required");
            }

            DateTime now = DateTime.UtcNow.AddHours(7);
            Models.Reservations.TableSession? activeSession = await Repo.GetFirstAsync<Models.Reservations.TableSession>(
                filter: ts => ts.TableId == tableId && ts.IsActive && ts.StartedAt <= now,
                orderBy: q => q.OrderByDescending(ts => ts.StartedAt),
                includeProperties: "Table"
            );

            if (activeSession == null)
            {
                throw new AppException("No active table session found for this table");
            }

            string tableDisplay = string.IsNullOrWhiteSpace(activeSession.Table?.Code)
                ? tableId.ToString()
                : activeSession.Table.Code;

            List<RestaurantNotification> createdNotifications = new List<RestaurantNotification>();

            if (!activeSession.OrderId.HasValue)
            {
                IList<ApplicationUser> staffUsers = await userManager.GetUsersInRoleAsync("Staff");
                if (staffUsers == null || staffUsers.Count == 0)
                {
                    throw new AppException("No staff found to receive payment request");
                }

                foreach (ApplicationUser staff in staffUsers)
                {
                    RestaurantNotification notification = new RestaurantNotification
                    {
                        RecipientId = staff.Id.ToString(),
                        NotificationType = NotificationType.PAYMENT.ToString(),
                        IsBroadcast = false,
                        Title = title.Trim(),
                        Message = $"{tableDisplay}",
                        Priority = NotificationPriority.HIGH.ToString(),
                        IsPublished = true,
                        ExpiryDate = DateTime.UtcNow.AddHours(9)
                    };

                    RestaurantNotification created = await CreateNotification(notification, userId);
                    createdNotifications.Add(created);
                }

                return createdNotifications;
            }

            DataTranferObjects.Orders.Order? order = await orderService.GetOrderByTableId(tableId);
            if (order == null || !order.Id.HasValue)
            {
                throw new AppException("No active order found for this table");
            }

            if (string.IsNullOrWhiteSpace(order.ModifiedBy))
            {
                throw new AppException("This order has no assigned staff (ModifiedBy)");
            }

            string orderDisplay = string.IsNullOrWhiteSpace(order.Reference)
                ? order.Id.Value.ToString()
                : order.Reference;

            RestaurantNotification singleNotification = new RestaurantNotification
            {
                RecipientId = order.ModifiedBy,
                NotificationType = NotificationType.PAYMENT.ToString(),
                IsBroadcast = false,
                Title = title.Trim(),
                Message = $"{tableDisplay}",
                Priority = NotificationPriority.HIGH.ToString(),
                IsPublished = true,
                ExpiryDate = DateTime.UtcNow.AddHours(9)
            };

            RestaurantNotification singleCreated = await CreateNotification(singleNotification, userId);
            createdNotifications.Add(singleCreated);

            return createdNotifications;
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
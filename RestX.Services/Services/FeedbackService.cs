using AutoMapper;
using Microsoft.AspNetCore.Http;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Feedback;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Feedbacks;
using RestX.Models.Enum;
using RestX.Models.Feedbacks;
using RestX.Models.Orders;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class FeedbackService : BaseService, IFeedbackService
    {
        private readonly ICloudinaryService cloudinaryService;
        private readonly IMapper mapper;
        public FeedbackService(
            ICloudinaryService cloudinaryService,
            IRepository repo,
            IRedisService redisService,
            IMapper mapper,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.cloudinaryService = cloudinaryService;
            this.mapper = mapper;
        }

        public async Task<FeedbackItem> CreateFeedback(Guid orderId, Guid customerId, FeedbackCreate request)
        {
            if (customerId == Guid.Empty)
            {
                throw new AppException("Customer not found");
            }

            Order? order = await Repo.GetByIdAsync<Order>(orderId);
            if (order == null)
            {
                throw new AppException("Order not found");
            }

            if (order.OrderStatusId != (int)OrderStatus.Completed)
            {
                throw new AppException("Only completed orders can be reviewed");
            }

            bool hasFeedback = await Repo.GetExistsAsync<Feedback>(f => f.OrderId == orderId);
            if (hasFeedback)
            {
                throw new AppException("This order already has feedback");
            }

            Feedback feedback = new Feedback
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                CustomerId = customerId,
                Rating = request.Rating,
                Comment = request.Comment,
                IsAnonymous = request.IsAnonymous,
                IsPublished = false
            };

            await Repo.CreateAsync(feedback);

            await CreateFeedbackImages(feedback.Id, request.Images);

            Feedback? created = await Repo.GetFirstAsync<Feedback>(
                filter: f => f.Id == feedback.Id,
                includeProperties: "Order,Customer.ApplicationUser,FeedbackImages");

            if (created == null)
            {
                throw new AppException("Failed to create feedback");
            }

            return MapFeedback(created);
        }

        public async Task<FeedbackItem?> GetFeedbackById(Guid id, Guid? callerCustomerId = null, bool isAdmin = false)
        {
            Feedback? feedback = await Repo.GetFirstAsync<Feedback>(
                filter: f => f.Id == id,
                includeProperties: "Order,Customer.ApplicationUser,FeedbackImages");

            if (feedback == null)
            {
                return null;
            }

            bool isOwner = callerCustomerId.HasValue && feedback.CustomerId == callerCustomerId.Value;

            if (!feedback.IsPublished && !isOwner && !isAdmin)
            {
                return null;
            }

            return MapFeedback(feedback);
        }

        public async Task<FeedbackItem> UpdateFeedback(Guid id, Guid callerCustomerId, FeedbackUpdate request, bool isAdmin = false)
        {
            Feedback? feedback = await Repo.GetFirstAsync<Feedback>(
                filter: f => f.Id == id,
                includeProperties: "Order,Customer.ApplicationUser,FeedbackImages");

            if (feedback == null)
            {
                throw new AppException("Feedback not found");
            }

            if (!isAdmin && feedback.CustomerId != callerCustomerId)
            {
                throw new AppException("You are not allowed to update this feedback");
            }

            if (!isAdmin && request.IsPublished.HasValue)
            {
                throw new AppException("Only admin can change publish status");
            }

            if (request.Rating.HasValue)
            {
                feedback.Rating = request.Rating.Value;
            }

            if (request.Comment != null)
            {
                feedback.Comment = request.Comment;
            }

            if (request.IsAnonymous.HasValue)
            {
                feedback.IsAnonymous = request.IsAnonymous.Value;
            }

            if (isAdmin && request.IsPublished.HasValue)
            {
                feedback.IsPublished = request.IsPublished.Value;
            }

            if (request.RemoveImageIds != null && request.RemoveImageIds.Any())
            {
                List<FeedbackImage> imagesToRemove = feedback.FeedbackImages
                    .Where(i => request.RemoveImageIds.Contains(i.Id))
                    .ToList();

                foreach (FeedbackImage image in imagesToRemove)
                {
                    Repo.Delete<FeedbackImage>(image);
                }
            }

            await CreateFeedbackImages(feedback.Id, request.NewImages);

            Repo.Update(feedback);
            await Repo.SaveAsync();

            Feedback? updated = await Repo.GetFirstAsync<Feedback>(
                filter: f => f.Id == id,
                includeProperties: "Order,Customer.ApplicationUser,FeedbackImages");

            if (updated == null)
            {
                throw new AppException("Feedback not found after update");
            }

            return MapFeedback(updated);
        }

        public async Task DeleteFeedback(Guid id, Guid callerCustomerId, bool isAdmin = false)
        {
            Feedback? feedback = await Repo.GetByIdAsync<Feedback>(id);
            if (feedback == null)
            {
                return;
            }

            if (!isAdmin && feedback.CustomerId != callerCustomerId)
            {
                throw new AppException("You are not allowed to delete this feedback");
            }

            Repo.Delete<Feedback>(feedback);
            await Repo.SaveAsync();
        }

        public async Task<PaginatedResult<FeedbackItem>> GetAdminFeedbacks(FeedbackFilterParams filter)
        {
            if (filter.PageNumber <= 0) filter.PageNumber = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;
            if (filter.PageSize > 100) filter.PageSize = 100;

            List<Feedback> feedbacks = (await Repo.GetAllAsync<Feedback>(
                includeProperties: "Order,Customer.ApplicationUser,FeedbackImages")).ToList();

            IEnumerable<Feedback> query = feedbacks;

            if (filter.OrderId.HasValue)
            {
                query = query.Where(f => f.OrderId == filter.OrderId.Value);
            }

            if (filter.CustomerId.HasValue)
            {
                query = query.Where(f => f.CustomerId == filter.CustomerId.Value);
            }

            if (filter.IsPublished.HasValue)
            {
                query = query.Where(f => f.IsPublished == filter.IsPublished.Value);
            }

            if (filter.MinRating.HasValue)
            {
                query = query.Where(f => f.Rating >= filter.MinRating.Value);
            }

            if (filter.MaxRating.HasValue)
            {
                query = query.Where(f => f.Rating <= filter.MaxRating.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim().ToLowerInvariant();
                query = query.Where(f =>
                    (f.Comment ?? string.Empty).ToLowerInvariant().Contains(search) ||
                    (f.Order.Reference ?? string.Empty).ToLowerInvariant().Contains(search) ||
                    (f.Customer.ApplicationUser.FullName ?? string.Empty).ToLowerInvariant().Contains(search));
            }

            query = (filter.SortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "rating" => filter.SortDescending ? query.OrderByDescending(f => f.Rating) : query.OrderBy(f => f.Rating),
                "createddate" => filter.SortDescending ? query.OrderByDescending(f => f.CreatedDate) : query.OrderBy(f => f.CreatedDate),
                _ => filter.SortDescending ? query.OrderByDescending(f => f.CreatedDate) : query.OrderBy(f => f.CreatedDate)
            };

            List<Feedback> filtered = query.ToList();
            int totalCount = filtered.Count;
            int totalActive = filtered.Count(f => f.IsPublished);
            int totalInactive = filtered.Count(f => !f.IsPublished);

            List<FeedbackItem> items = filtered
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(MapFeedback)
                .ToList();

            PaginatedResult<FeedbackItem> result = new PaginatedResult<FeedbackItem>(
                items,
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            result.TotalActive = totalActive;
            result.TotalInactive = totalInactive;

            return result;
        }

        private async Task CreateFeedbackImages(Guid feedbackId, IFormFile[]? images)
        {
            if (images == null || !images.Any())
            {
                return;
            }

            string folderName = $"{(CurrentTenant?.Name ?? "tenant").Replace(" ", string.Empty)}/feedbacks";

            for (int i = 0; i < images.Length; i++)
            {
                IFormFile file = images[i];
                using Stream stream = file.OpenReadStream();
                CloudinaryUploadResult uploadResult = await cloudinaryService.UploadAsync(
                    stream,
                    file.FileName,
                    folderName,
                    $"{feedbackId}_{i}",
                    overwrite: false);

                FeedbackImage image = new FeedbackImage
                {
                    Id = Guid.NewGuid(),
                    FeedbackId = feedbackId,
                    ImageUrl = uploadResult.Url,
                    DisplayOrder = i,
                    IsCover = i == 0
                };

                await Repo.CreateAsync(image);
            }
        }

        private FeedbackItem MapFeedback(Feedback feedback)
        {
            FeedbackCustomerInfo? customer = null;

            if (!feedback.IsAnonymous)
            {
                customer = new FeedbackCustomerInfo
                {
                    Id = feedback.CustomerId,
                    FullName = feedback.Customer?.ApplicationUser?.FullName ?? string.Empty,
                    AvatarUrl = feedback.Customer?.ApplicationUser?.AvatarUrl
                };
            }

            List<FeedbackImageItem> images = feedback.FeedbackImages
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new FeedbackImageItem
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    DisplayOrder = i.DisplayOrder,
                    IsCover = i.IsCover
                })
                .ToList();

            return mapper.Map<FeedbackItem>(feedback);
        }
    }
}
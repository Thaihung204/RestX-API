using AutoMapper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RestX.BLL.DataTranferObjects.Category;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using StackExchange.Redis;

namespace RestX.BLL.Services
{
    public class CategoryService : BaseService, ICategoryService
    {
        private readonly IMapper mapper;
        private readonly ICloudinaryService cloudinaryService;

        public CategoryService(
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
            this.cloudinaryService = cloudinaryService;
        }

        private string GetCacheKey()
            => $"{CurrentTenant.Hostname}:Category";

        public async Task<IEnumerable<CategoryItem>> GetAllCategories()
        {
            var categories = await RedisService.GetAsync<List<Category>>(GetCacheKey());
            if (categories == null)
            {
                categories = (await Repo.GetAllAsync<Category>(
                    orderBy: q => q.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name),
                    includeProperties: "ParentCategory,SubCategories"
                )).ToList();

                await RedisService.SetAsync(GetCacheKey(), categories);
            }

            return mapper.Map<List<CategoryItem>>(categories);
        }

        public async Task<CategoryItem?> GetCategoryById(Guid id)
        {
            var category = await Repo.GetOneAsync<Category>(
                filter: c => c.Id == id,
                includeProperties: "ParentCategory,SubCategories"
            );
            return mapper.Map<CategoryItem>(category);
        }

        public async Task<Guid> UpsertCategory(CategoryItem dto)
        {
            await ValidateCategoryData(dto);

            Category category;

            if (dto.Id != null)
            {
                category = await Repo.GetByIdAsync<Category>(dto.Id.Value);
                if (category == null)
                {
                    throw new AppException("Category not found");
                }

                category.Name = dto.Name.Trim();
                category.Description = (dto.Description ?? string.Empty).Trim();
                category.ParentId = dto.ParentId;
                category.IsActive = dto.IsActive;
                category.DisplayOrder = dto.DisplayOrder;

                await cloudinaryService.DeleteAsync($"{CurrentTenant.Name.Replace(" ", "")}/categories/{category.Id}");

                if (dto.File == null)
                {
                    category.ImageUrl = dto.ImageUrl;
                }
                else
                {
                    string newImageUrl = await HandleCategoryImageUpload(dto.File, category.Id);
                    category.ImageUrl = newImageUrl;
                }

                Repo.Update(category);
                await Repo.SaveAsync();
                await RedisService.RemoveAsync(GetCacheKey());

                return category.Id;
            }

            int displayOrder = dto.DisplayOrder;
            if (displayOrder <= 0)
            {
                Category? lastCategory = (await Repo.GetAllAsync<Category>(
                    orderBy: q => q.OrderByDescending(c => c.DisplayOrder),
                    take: 1
                )).FirstOrDefault();

                displayOrder = (lastCategory?.DisplayOrder ?? 0) + 1;
            }

            category = new Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = (dto.Description ?? string.Empty).Trim(),
                ParentId = dto.ParentId,
                IsActive = dto.IsActive,
                ImageUrl = dto.ImageUrl,
                DisplayOrder = displayOrder
            };

            if (dto.File != null)
            {
                category.ImageUrl = await HandleCategoryImageUpload(dto.File, category.Id);
            }

            Repo.Create(category);
            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey());

            return category.Id;
        }

        private async Task ValidateCategoryData(CategoryItem dto)
        {
            if (dto == null)
            {
                throw new AppException("Category data is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 255)
            {
                throw new AppException("Category name is required and must be <= 255 characters.");
            }

            string description = (dto.Description ?? string.Empty).Trim();
            if (description.Length > 1000)
            {
                throw new AppException("Category description must be <= 1000 characters.");
            }

            if (dto.Id.HasValue && dto.ParentId.HasValue && dto.Id.Value == dto.ParentId.Value)
            {
                throw new AppException("Category cannot be its own parent.");
            }

            if (dto.ParentId.HasValue)
            {
                Category? parent = await Repo.GetByIdAsync<Category>(dto.ParentId.Value);
                if (parent == null)
                {
                    throw new AppException("Parent category not found.");
                }
            }

            string normalizedName = dto.Name.Trim().ToLowerInvariant();
            bool duplicateName = await Repo.GetExistsAsync<Category>(c =>
                c.Name.ToLower() == normalizedName
                && c.ParentId == dto.ParentId
                && (!dto.Id.HasValue || c.Id != dto.Id.Value));

            if (duplicateName)
            {
                throw new AppException("Category name already exists in the same parent.");
            }
        }
        public async Task UpdateDisplayOrder(List<CategoryItem> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                throw new AppException("Danh sách category không hợp lệ.");
            }

            List<CategoryItem> invalidItems = categories
                .Where(c => !c.Id.HasValue || c.DisplayOrder <= 0)
                .ToList();

            if (invalidItems.Count > 0)
            {
                throw new AppException("Danh sách category có phần tử thiếu Id hoặc DisplayOrder không hợp lệ.");
            }

            List<Guid> ids = categories
                .Select(c => c.Id!.Value)
                .Distinct()
                .ToList();

            if (ids.Count != categories.Count)
            {
                throw new AppException("Danh sách category bị trùng Id.");
            }

            List<int> displayOrders = categories
                .Select(c => c.DisplayOrder)
                .Distinct()
                .ToList();

            if (displayOrders.Count != categories.Count)
            {
                throw new AppException("Danh sách category bị trùng DisplayOrder.");
            }

            List<Category> dbCategories = (await Repo.GetAsync<Category>(c => ids.Contains(c.Id))).ToList();
            if (dbCategories.Count != ids.Count)
            {
                throw new AppException("Một hoặc nhiều category không tồn tại.");
            }

            foreach (CategoryItem item in categories)
            {
                Category category = dbCategories.First(c => c.Id == item.Id!.Value);
                category.DisplayOrder = item.DisplayOrder;
                Repo.Update(category);
            }

            await Repo.SaveAsync();
            await RedisService.RemoveAsync(GetCacheKey());
        }
        private async Task<string> HandleCategoryImageUpload(IFormFile file, Guid categoryId)
        {
            using var stream = file.OpenReadStream();

            var uploadResult = await cloudinaryService.UploadAsync(
                fileStream: stream,
                fileName: file.FileName,
                folder: $"{CurrentTenant.Name.Replace(" ", "")}/categories/",
                publicId: categoryId.ToString(), 
                overwrite: true
            );

            return uploadResult.Url;
        }

        public async Task DeleteCategory(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new AppException("Category id is required.");
            }

            Category? category = await Repo.GetByIdAsync<Category>(id);
            if (category == null)
            {
                throw new AppException("Category not found.");
            }

            bool hasSubCategories = await Repo.GetExistsAsync<Category>(c => c.ParentId == id);
            if (hasSubCategories)
            {
                throw new AppException("Cannot delete category because it has sub-categories.");
            }

            bool hasDishes = await Repo.GetExistsAsync<Dish>(d => d.CategoryId == id);
            if (hasDishes)
            {
                throw new AppException("Cannot delete category because it is being used by one or more dishes.");
            }

            await cloudinaryService.DeleteAsync($"{CurrentTenant.Name.Replace(" ", "")}/categories/{id}");

            Repo.Delete<Category>(id);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync(GetCacheKey());
        }
    }
}
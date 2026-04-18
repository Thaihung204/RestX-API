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
            Category category;

            if (dto.Id != null)
            {
                category = await Repo.GetByIdAsync<Category>(dto.Id.Value);
                category.Name = dto.Name;
                category.Description = dto.Description;
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
                Name = dto.Name,
                Description = dto.Description,
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

        public async Task UpdateDisplayOrder(List<CategoryItem> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                throw new AppException("Danh sách category không hợp lệ.");
            }

            List<Guid> ids = categories
                .Where(c => c.Id.HasValue)
                .Select(c => c.Id!.Value)
                .Distinct()
                .ToList();

            if (ids.Count != categories.Count)
            {
                throw new AppException("Danh sách category có phần tử thiếu Id hoặc bị trùng Id.");
            }

            List<Category> dbCategories = (await Repo.GetAsync<Category>(c => ids.Contains(c.Id))).ToList();
            if (dbCategories.Count != ids.Count)
            {
                throw new AppException("Một hoặc nhiều category không tồn tại.");
            }

            for (int i = 0; i < categories.Count; i++)
            {
                Guid categoryId = categories[i].Id!.Value;
                Category category = dbCategories.First(c => c.Id == categoryId);
                category.DisplayOrder = i + 1;
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
            var category = await Repo.GetByIdAsync<Category>(id);
            if (category == null)
                return;
            await cloudinaryService.DeleteAsync($"{CurrentTenant.Name.Replace(" ", "")}/categories/{id}/{id}");

            Repo.Delete<Category>(id);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync(GetCacheKey());
        }
    }
}
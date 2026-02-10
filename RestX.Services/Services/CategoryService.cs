using AutoMapper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RestX.BLL.DataTranferObjects.Category;
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
            => $"{CurrentTenant?.Id}:category";

        public async Task<IEnumerable<CategoryItem>> GetAllCategories()
        {
            var categories = await RedisService.GetAsync<List<Category>>(GetCacheKey());
            if (categories == null)
            {
                Console.WriteLine("Repo:", Repo.ToString());
                categories = (await Repo.GetAllAsync<Category>(
                    orderBy: q => q.OrderBy(c => c.Name),
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

                await cloudinaryService.DeleteAsync($"{CurrentTenant.Name.Replace(" ", "")}/categories/{category.Id}");

                if (dto.File == null) category.ImageUrl = dto.ImageUrl;
                else
                {
                    var newImageUrl = await HandleCategoryImageUpload(dto.File, category.Id);
                    category.ImageUrl = newImageUrl;
                }

                Repo.Update(category);
                await Repo.SaveAsync();

                await RedisService.RemoveAsync(GetCacheKey());

                return category.Id;
            }

            category = new Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                ParentId = dto.ParentId,
                IsActive = dto.IsActive,
                ImageUrl = dto.ImageUrl
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
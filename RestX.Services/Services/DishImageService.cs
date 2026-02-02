using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.Interfaces;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Services
{
    public class DishImageService : BaseService, IDishImageService
    {
        private readonly ICloudinaryService cloudinaryService;

        public DishImageService(
            ICloudinaryService cloudinaryService,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.cloudinaryService = cloudinaryService;
        }

        public async Task HandleDishImagesAsync(DishUpsert model, Dish dish)
        {
            var folder = $"dishes/{dish.Id}";

            await HandleMainImage(model, dish, folder);
            await HandleSubImages(model, dish, folder);

            await Repo.SaveAsync();
        }

        // ======================
        // MAIN IMAGE
        // ======================
        private async Task HandleMainImage(
            DishUpsert model,
            Dish dish,
            string folder)
        {
            if (model.MainImage == null || model.MainImage.Length == 0)
                return;

            // deactivate old main images
            var oldMainImages = await Repo.GetAsync<DishImage>(
                x => x.DishId == dish.Id &&
                     x.ImageType == DishImageType.Main &&
                     x.IsActive
            );

            foreach (var img in oldMainImages)
            {
                img.IsActive = false;
                Repo.Update(img);
            }

            // create DishImage first to get Id
            var mainImage = new DishImage
            {
                Id = Guid.NewGuid(),
                DishId = dish.Id,
                ImageType = DishImageType.Main,
                DisplayOrder = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await Repo.CreateAsync(mainImage);
            await Repo.SaveAsync();

            using var stream = model.MainImage.OpenReadStream();

            var upload = await cloudinaryService.UploadAsync(
                fileStream: stream,
                fileName: model.MainImage.FileName,
                folder: folder,
                publicId: mainImage.Id.ToString(),
                overwrite: true
            );

            mainImage.ImageUrl = upload.Url;
            Repo.Update(mainImage);
        }

        // ======================
        // SUB IMAGES
        // ======================
        private async Task HandleSubImages(
            DishUpsert model,
            Dish dish,
            string folder)
        {
            if (model.SubImages == null || !model.SubImages.Any())
                return;

            var existingImages = await Repo.GetAsync<DishImage>(
                x => x.DishId == dish.Id && x.IsActive
            );

            var displayImage = existingImages.Any()
                ? existingImages.Max(x => x.DisplayOrder)
                : 1;

            foreach (var file in model.SubImages)
            {
                if (file == null || file.Length == 0)
                    continue;

                displayImage++;

                var subImage = new DishImage
                {
                    Id = Guid.NewGuid(),
                    DishId = dish.Id,
                    ImageType = DishImageType.Sub,
                    DisplayOrder = displayImage,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await Repo.CreateAsync(subImage);
                await Repo.SaveAsync();

                using var stream = file.OpenReadStream();

                var upload = await cloudinaryService.UploadAsync(
                    fileStream: stream,
                    fileName: file.FileName,
                    folder: folder,
                    publicId: subImage.Id.ToString()
                );

                subImage.ImageUrl = upload.Url;
                Repo.Update(subImage);
            }
        }

        public async Task DeleteDishImageAsync(Guid dishImageId)
        {
            var dishImage = await Repo.GetByIdAsync<DishImage>(dishImageId);
            if (dishImage == null)
                return;

            var publicId = $"dishes/{dishImage.DishId}/{dishImage.Id}";

            await cloudinaryService.DeleteAsync(publicId);

            dishImage.IsActive = false;
            Repo.Update(dishImage);

            await Repo.SaveAsync();
        }
        public async Task DeleteAllByDishIdAsync(Guid dishId)
        {
            var images = await Repo.GetAsync<DishImage>(
                x => x.DishId == dishId && x.IsActive
            );

            foreach (var img in images)
            {
                var publicId = $"dishes/{img.DishId}/{img.Id}";
                await cloudinaryService.DeleteAsync(publicId);

                img.IsActive = false;
                Repo.Update(img);
            }

            await Repo.SaveAsync();
        }

    }

}

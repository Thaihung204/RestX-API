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
            var mainImage = (await Repo.GetAsync<DishImage>(
                x => x.DishId == dish.Id
                  && x.ImageType == DishImageType.Main
                  && x.IsActive
            )).FirstOrDefault();

            if (model.MainImage == null)
            {
                if (mainImage != null)
                {
                    await DeleteDishImageAsync(mainImage.Id);
                    Repo.Delete(mainImage);
                }

                return;
            }

            if (mainImage == null)
            {
                mainImage = new DishImage
                {
                    Id = Guid.NewGuid(),
                    DishId = dish.Id,
                    ImageType = DishImageType.Main,
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await Repo.CreateAsync(mainImage);
            }

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

            var oldImages = await Repo.GetAsync<DishImage>(
                x => x.DishId == dish.Id
                  && x.ImageType == DishImageType.Sub
            );

            foreach (var img in oldImages)
            {
                await DeleteDishImageAsync(img.Id);
                Repo.Delete(img);
            }

            if (model.SubImages == null)
                return;

            int displayOrder = 1;

            foreach (var file in model.SubImages)
            {
                if (file == null || file.Length == 0)
                    continue;

                var subImage = new DishImage
                {
                    Id = Guid.NewGuid(),
                    DishId = dish.Id,
                    ImageType = DishImageType.Sub,
                    DisplayOrder = displayOrder++,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await Repo.CreateAsync(subImage);

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

            var publicId = $"dishes/{dishImage.DishId}/{dishImage.Id}";

            await cloudinaryService.DeleteAsync(publicId);

        }
        public async Task DeleteAllByDishIdAsync(Guid dishId)
        {
            var prefix = $"dishes/{dishId}";
            await cloudinaryService.DeleteFolderImageByPrefix(prefix);
        }

    }

}

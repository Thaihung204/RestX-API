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
            var existingImages = (await Repo.GetAsync<DishImage>(
                x => x.DishId == dish.Id
                  && x.ImageType == DishImageType.Sub
                  && x.IsActive
            )).ToList();

            if (model.SubImages == null || !model.SubImages.Any())
            {
                foreach (var oldImage in existingImages)
                {
                    await DeleteDishImageAsync(oldImage.Id);
                    Repo.Delete(oldImage);
                }

                return;
            }

            var incomingIds = model.SubImages
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            foreach (var oldImage in existingImages)
            {
                if (!incomingIds.Contains(oldImage.Id))
                {
                    await DeleteDishImageAsync(oldImage.Id);
                    Repo.Delete(oldImage);
                }
            }

            existingImages = (await Repo.GetAsync<DishImage>(
                x => x.DishId == dish.Id
                  && x.ImageType == DishImageType.Sub
                  && x.IsActive
            )).ToList();

            foreach (var item in model.SubImages)
            {
                DishImage subImage;

                if (item.Id.HasValue)
                {
                    subImage = existingImages.FirstOrDefault(x => x.Id == item.Id.Value);
                    if (subImage == null) continue;
                }

                else
                {
                    subImage = new DishImage
                    {
                        Id = Guid.NewGuid(),
                        DishId = dish.Id,
                        ImageType = DishImageType.Sub,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    await Repo.CreateAsync(subImage);
                    existingImages.Add(subImage);
                }

                if (item.File != null && item.File.Length > 0)
                {
                    using var stream = item.File.OpenReadStream();

                    var upload = await cloudinaryService.UploadAsync(
                        fileStream: stream,
                        fileName: item.File.FileName,
                        folder: folder,
                        publicId: subImage.Id.ToString(),
                        overwrite: item.Id.HasValue
                    );

                    subImage.ImageUrl = upload.Url;
                }

                subImage.DisplayOrder = item.DisplayOrder;

                Repo.Update(subImage);
            }

            var ordered = existingImages
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].DisplayOrder = i + 1;
                Repo.Update(ordered[i]);
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

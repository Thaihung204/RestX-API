using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestX.BLL.DataTranferObjects.Combo;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using System.Data;
using System.Text;

namespace RestX.BLL.Services
{
    public class DishService : BaseService, IDishService
    {
        private readonly ICloudinaryService cloudinaryService;
        private readonly IMapper mapper;

        public DishService(ICloudinaryService cloudinaryService, IMapper mapper, IRepository repo, IRedisService redisService, IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
            this.cloudinaryService = cloudinaryService;
        }

        public async Task<DishSearchResult> GetAllDishes(DishSearch model)
        {
            var result = new DishSearchResult();

            var query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM dbo.Dishes d
                WHERE 1 = 1
            ");

            var countParams = new List<SqlParameter>();
            var queryParams = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(model.SearchText))
            {
                query.Append(@"
                    AND (
                        d.Name LIKE '%' + @SearchText + '%'
                        OR d.Description LIKE '%' + @SearchText + '%'
                    )
                ");

                countParams.Add(new SqlParameter("SearchText", SqlDbType.NVarChar, 2000) { Value = model.SearchText });
                queryParams.Add(new SqlParameter("SearchText", SqlDbType.NVarChar, 2000) { Value = model.SearchText });
            }

            if (model.CategoryId.HasValue)
            {
                query.Append(" AND d.CategoryId = @CategoryId ");

                countParams.Add(new SqlParameter("CategoryId", SqlDbType.UniqueIdentifier) { Value = model.CategoryId.Value });
                queryParams.Add(new SqlParameter("CategoryId", SqlDbType.UniqueIdentifier) { Value = model.CategoryId.Value });
            }

            void AddBoolFilter(string column, string param, bool? value)
            {
                if (!value.HasValue) return;

                query.Append($" AND {column} = @{param} ");

                countParams.Add(new SqlParameter(param, SqlDbType.Bit) { Value = value.Value });
                queryParams.Add(new SqlParameter(param, SqlDbType.Bit) { Value = value.Value });
            }

            AddBoolFilter("d.IsVegetarian", "IsVegetarian", model.IsVegetarian);
            AddBoolFilter("d.IsSpicy", "IsSpicy", model.IsSpicy);
            AddBoolFilter("d.IsBestSeller", "IsBestSeller", model.IsBestSeller);
            AddBoolFilter("d.IsActive", "IsActive", model.IsActive);

            if (model.PriceFrom.HasValue)
            {
                query.Append(" AND d.Price >= @PriceFrom ");

                countParams.Add(new SqlParameter("PriceFrom", SqlDbType.Decimal) { Value = model.PriceFrom.Value });
                queryParams.Add(new SqlParameter("PriceFrom", SqlDbType.Decimal) { Value = model.PriceFrom.Value });
            }

            if (model.PriceTo.HasValue)
            {
                query.Append(" AND d.Price <= @PriceTo ");

                countParams.Add(new SqlParameter("PriceTo", SqlDbType.Decimal) { Value = model.PriceTo.Value });
                queryParams.Add(new SqlParameter("PriceTo", SqlDbType.Decimal) { Value = model.PriceTo.Value });
            }

            var countQuery = query.ToString().Replace("#SELECT#", "COUNT(1)");

            var totalCount = await Repo.ExecuteSqlCommandAsync<int>(
                countQuery,
                countParams.Any() ? countParams.Cast<object>().ToArray() : null
            );

            result.TotalCount = totalCount;
            result.Page = model.Page;
            result.ItemsPerPage = model.ItemsPerPage;
            result.TotalPages = (int)Math.Ceiling((decimal)totalCount / model.ItemsPerPage);

            var skip = model.Page <= 1 ? 0 : (model.Page - 1) * model.ItemsPerPage;

            var selectItems = @"
                d.Id,
                d.CategoryId,
                d.Name,
                d.Description,
                d.Price,
                d.Unit,
                d.Quantity,
                d.IsActive,
                d.AutoDisableByStock,
                d.IsVegetarian,
                d.IsSpicy,
                d.IsBestSeller
            ";

            var mainQuery = query.ToString().Replace("#SELECT#", selectItems);

            mainQuery += model.SortBy switch
            {
                "name_asc" => " ORDER BY d.IsActive DESC, d.Name ASC",
                "name_desc" => " ORDER BY d.IsActive DESC, d.Name DESC",
                "price_asc" => " ORDER BY d.IsActive DESC, d.Price ASC",
                "price_desc" => " ORDER BY d.IsActive DESC, d.Price DESC",
                "created_asc" => " ORDER BY d.IsActive DESC, d.CreatedDate ASC",
                _ => " ORDER BY d.IsActive DESC, d.CreatedDate DESC"
            };

            mainQuery += $" OFFSET {skip} ROWS FETCH NEXT {model.ItemsPerPage} ROWS ONLY";

            var dishes = await Repo.ExecuteSqlSelectAsync<DishItem>(
                mainQuery,
                queryParams.Any() ? queryParams.Cast<object>().ToArray() : null
            );

            if (dishes.Count > 0)
            {
                var ids = dishes.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();
                var idParams = ids
                    .Select((id, i) => new SqlParameter($"DishId{i}", SqlDbType.UniqueIdentifier) { Value = id })
                    .ToList();

                var inClause = string.Join(", ", idParams.Select(p => "@" + p.ParameterName));

                // Get ALL images (not just main)
                var imgQuery = $@"
                    SELECT
                        di.Id,
                        di.DishId,
                        di.ImageUrl,
                        di.ImageType,
                        di.DisplayOrder,
                        di.IsActive
                    FROM dbo.DishImages di
                    WHERE di.DishId IN ({inClause})
                    ORDER BY di.DishId, di.DisplayOrder ASC, di.Id ASC
                ";

                var images = await Repo.ExecuteSqlSelectAsync<DishImage>(
                    imgQuery,
                    idParams.Cast<object>().ToArray()
                );

                var imagesByDishId = images
                    .GroupBy(x => x.DishId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var dish in dishes)
                {
                    dish.Images = new List<DishImageItem>();

                    if (dish.Id is null) continue;

                    if (!imagesByDishId.TryGetValue(dish.Id.Value, out var imgs)) continue;

                    dish.Images = imgs.Select(img => new DishImageItem
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl,
                        ImageType = img.ImageType,
                        DisplayOrder = img.DisplayOrder,
                        IsActive = img.IsActive
                    }).ToList();
                }
            }

            result.Dishes = dishes;
            return result;
        }

        public async Task<DishItem> GetDishById(Guid id)
        {
            var dish = await Repo.GetOneAsync<Dish>(
                filter: d => d.Id == id,
                includeProperties: "Category,DishImages"
            );

            DishItem dishItem;
            try
            {
                dishItem = mapper.Map<DishItem>(dish);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Console.Error.WriteLine(ex.ToString());
                throw;
            }

            dishItem.Images = dish.DishImages?
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .Select(x => new DishImageItem
                {
                    Id = x.Id,
                    ImageUrl = x.ImageUrl,
                    ImageType = x.ImageType,
                    DisplayOrder = x.DisplayOrder,
                    IsActive = x.IsActive
                })
                .ToList() ?? new List<DishImageItem>();

            return dishItem;
        }

        public async Task<List<DishItem>> GetDishByCategory(Guid categoryId)
        {
            List<Dish> dishes = (await Repo.GetAsync<Dish>(
                filter: d => d.CategoryId == categoryId && d.IsActive,
                orderBy: q => q.OrderBy(d => d.Name),
                includeProperties: "DishImages"
            )).ToList();

            List<DishItem> result = dishes.Select(d => new DishItem
            {
                Id = d.Id,
                CategoryId = d.CategoryId,
                Name = d.Name,
                Description = d.Description,
                Price = d.Price,
                Unit = d.Unit,
                Quantity = d.Quantity,
                IsVegetarian = d.IsVegetarian,
                IsSpicy = d.IsSpicy,
                IsBestSeller = d.IsBestSeller,
                IsActive = d.IsActive,
                AutoDisableByStock = d.AutoDisableByStock,
                Images = d.DishImages
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.DisplayOrder)
                    .ThenBy(i => i.Id)
                    .Select(i => new DishImageItem
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        ImageType = i.ImageType,
                        DisplayOrder = i.DisplayOrder,
                        IsActive = i.IsActive
                    })
                    .ToList()
            }).ToList();

            return result;
        }

        public async Task<Guid> UpsertDish(DishItem dishItem)
        {
            await ValidateDishData(dishItem);

            Dish dish;

            if (dishItem.Id == null)
            {
                dish = mapper.Map<Dish>(dishItem);
                Repo.Create(dish);
            }
            else
            {
                dish = await Repo.GetOneAsync<Dish>(
                    filter: x => x.Id == dishItem.Id,
                    includeProperties: "Category,DishImages");

                mapper.Map(dishItem, dish);
                Repo.Update(dish);
            }

            var currentImageIds = dish.DishImages.Select(x => x.Id).ToList();
            var incomingImageIds = dishItem.Images
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id.Value)
                .ToList();

            var idsToDelete = currentImageIds.Except(incomingImageIds).ToList();
            foreach (var id in idsToDelete)
            {
                await cloudinaryService.DeleteAsync($"{CurrentTenant.Name.Replace(" ", "")}/dishes/{dish.Id}/{id}");
                Repo.Delete<DishImage>(id);
            }

            var uploadTasks = new List<Task<DishImage>>();
            foreach (var dishImageItem in dishItem.Images)
            {
                if (dishImageItem.Id != null)
                {
                    var existingImg = dish.DishImages.FirstOrDefault(x => x.Id == dishImageItem.Id);
                    if (existingImg != null)
                    {
                        existingImg.DisplayOrder = dishImageItem.DisplayOrder ?? existingImg.DisplayOrder;
                        existingImg.ImageType = dishImageItem.ImageType ?? existingImg.ImageType;
                        existingImg.IsActive = dishImageItem.IsActive;
                        Repo.Update(existingImg);
                    }
                }
                else if (dishImageItem.File != null)
                {
                    var newImageId = Guid.NewGuid();
                    uploadTasks.Add(HandleImageUpload(dishImageItem, dish.Id, newImageId));
                }
            }

            if (uploadTasks.Any())
            {
                var newImages = await Task.WhenAll(uploadTasks);
                foreach (var newImg in newImages)
                {
                    Repo.Create(newImg);
                }
            }

            await Repo.SaveAsync();
            await RedisService.RemoveAsync($"{CurrentTenant.Hostname}:Menu");

            return dish.Id;
        }

        private async Task ValidateDishData(DishItem dishItem)
        {
            if (dishItem == null)
            {
                throw new AppException("Dish data is required.");
            }

            if (dishItem.CategoryId == Guid.Empty)
            {
                throw new AppException("Category is required.");
            }

            bool categoryExists = await Repo.GetExistsAsync<Category>(c => c.Id == dishItem.CategoryId);
            if (!categoryExists)
            {
                throw new AppException("Category not found.");
            }

            dishItem.Name = (dishItem.Name ?? string.Empty).Trim();
            dishItem.Description = (dishItem.Description ?? string.Empty).Trim();
            dishItem.Unit = (dishItem.Unit ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(dishItem.Name))
            {
                throw new AppException("Dish name is required.");
            }

            if (dishItem.Name.Length > 255)
            {
                throw new AppException("Dish name cannot exceed 255 characters.");
            }

            if (dishItem.Description.Length > 2000)
            {
                throw new AppException("Dish description cannot exceed 2000 characters.");
            }

            if (string.IsNullOrWhiteSpace(dishItem.Unit))
            {
                throw new AppException("Dish unit is required.");
            }

            if (dishItem.Unit.Length > 20)
            {
                throw new AppException("Dish unit cannot exceed 20 characters.");
            }

            if (dishItem.Price < 0)
            {
                throw new AppException("Dish price must be greater than or equal to 0.");
            }

            if (dishItem.Quantity < 0)
            {
                throw new AppException("Dish quantity must be greater than or equal to 0.");
            }

            string normalizedName = dishItem.Name.ToLower();
            bool duplicateName = await Repo.GetExistsAsync<Dish>(d =>
                d.CategoryId == dishItem.CategoryId
                && d.Name.ToLower() == normalizedName
                && (!dishItem.Id.HasValue || d.Id != dishItem.Id.Value));

            if (duplicateName)
            {
                throw new AppException("Dish name already exists in this category.");
            }
        }
        private async Task<DishImage> HandleImageUpload(DishImageItem dishImageItem, Guid dishId, Guid newImageId)
        {
            using var stream = dishImageItem.File.OpenReadStream();

            var uploadResult = await cloudinaryService.UploadAsync(
                fileStream: stream,
                fileName: dishImageItem.File.FileName,
                folder: $"{CurrentTenant.Name.Replace(" ", "")}/dishes/{dishId}",
                publicId: newImageId.ToString(),
                overwrite: true
            );

            return new DishImage
            {
                Id = newImageId,
                DishId = dishId,
                ImageUrl = uploadResult.Url,
                ImageType = dishImageItem.ImageType ?? DishImageType.Main,
                DisplayOrder = dishImageItem.DisplayOrder ?? 0,
                IsActive = dishImageItem.IsActive
            };
        }

        public async Task<bool> DeleteDish(Guid id)
        {
            var dish = await Repo.GetByIdAsync<Dish>(id);

            if (dish == null)
                return false;

            Repo.Delete<Dish>(id);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync($"{CurrentTenant.Hostname}:Menu");

            return true;
        }

        public async Task<List<MenuCategory>> GetMenu()
        {
            var cacheKey = $"{CurrentTenant.Hostname}:Menu";

            var cachedMenu = await RedisService.GetAsync<List<MenuCategory>>(cacheKey);
            if (cachedMenu != null)
                return cachedMenu;

            var dishes = await Repo.GetAsync<Dish>(
                filter: d => d.IsActive,
                includeProperties: "Category,DishImages"
            );

            var menuItems = dishes.Select(d =>
            {
                var mainImageUrl = d.DishImages?
                    .Where(x => x.IsActive && x.ImageType == DishImageType.Main)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Id)
                    .Select(x => x.ImageUrl)
                    .FirstOrDefault();

                return new MenuItem
                {
                    Id = d.Id,
                    CategoryId = d.CategoryId,
                    CategoryName = d.Category?.Name ?? string.Empty,
                    Name = d.Name,
                    Price = d.Price,
                    Description = d.Description,
                    ImageUrl = mainImageUrl,
                    IsBestSeller = d.IsBestSeller,
                    IsSpicy = d.IsSpicy,
                    IsVegetarian = d.IsVegetarian,
                };
            }).ToList();

            var categoryOrders = dishes
                .GroupBy(d => d.CategoryId)
                .ToDictionary(g => g.Key, g => g.First().Category?.DisplayOrder ?? 0);

            var menu = menuItems
                .GroupBy(x => new { x.CategoryId, x.CategoryName })
                .Select(g => new MenuCategory
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    Items = g.ToList()
                })
                .OrderBy(c => categoryOrders.ContainsKey(c.CategoryId) ? categoryOrders[c.CategoryId] : int.MaxValue)
                .ThenBy(c => c.CategoryName)
                .ToList();

            await RedisService.SetAsync(cacheKey, menu);

            return menu;
        }
        // Recipe methods
        public async Task<List<DishRecipeItem>> GetRecipesByDishId(Guid dishId)
        {
            var recipes = await Repo.GetAsync<DishRecipe>(
                filter: r => r.DishId == dishId,
                includeProperties: "Ingredient"
            );

            return mapper.Map<List<DishRecipeItem>>(recipes);
        }

        public async Task<DishRecipeItem?> GetRecipeById(Guid id)
        {
            var recipe = await Repo.GetByIdAsync<DishRecipe>(id);
            return recipe == null ? null : mapper.Map<DishRecipeItem>(recipe);
        }

        public async Task<Guid> CreateRecipe(DishRecipeItem item)
        {
            var recipe = new DishRecipe
            {
                DishId = item.DishId,
                IngredientId = item.IngredientId,
                Quantity = item.Quantity
            };

            await Repo.CreateAsync(recipe, null);
            return recipe.Id;
        }

        public async Task<Guid> UpdateRecipe(Guid id, DishRecipeItem item)
        {
            var recipe = await Repo.GetByIdAsync<DishRecipe>(id);
            if (recipe == null)
                return Guid.Empty;

            recipe.IngredientId = item.IngredientId;
            recipe.Quantity = item.Quantity;

            Repo.Update(recipe);
            await Repo.SaveAsync();

            return recipe.Id;
        }

        public async Task<bool> DeleteRecipe(Guid id)
        {
            var recipe = await Repo.GetByIdAsync<DishRecipe>(id);
            if (recipe == null)
                return false;

            Repo.Delete<DishRecipe>(id);
            await Repo.SaveAsync();

            return true;
        }

        public async Task<Guid> SetRecipes(Guid dishId, List<DishRecipeItem> items)
        {
            var existingRecipes = await Repo.GetAsync<DishRecipe>(filter: r => r.DishId == dishId);
            foreach (var recipe in existingRecipes)
            {
                Repo.Delete<DishRecipe>(recipe.Id);
            }

            foreach (var item in items)
            {
                var recipe = new DishRecipe
                {
                    DishId = dishId,
                    IngredientId = item.IngredientId,
                    Quantity = item.Quantity
                };
                await Repo.CreateAsync(recipe, null);
            }

            await Repo.SaveAsync();
            return dishId;
        }

        // Combo methods
        public async Task<List<ComboSummary>> GetAllCombos()
        {
            string cacheKey = $"{CurrentTenant.Hostname}:Combos";

            List<ComboSummary>? cachedCombos = await RedisService.GetAsync<List<ComboSummary>>(cacheKey);
            if (cachedCombos != null)
            {
                return cachedCombos;
            }

            List<MealCombo> combos = (await Repo.GetAllAsync<MealCombo>(
                orderBy: q => q.OrderByDescending(c => c.CreatedDate),
                includeProperties: "ComboDetails.Dish"
            )).ToList();

            List<ComboSummary> result = combos
                .Select(MapToComboSummary)
                .ToList();

            await RedisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }

        public async Task<List<ComboSummary>> GetActiveCombos()
        {
            string cacheKey = $"{CurrentTenant.Hostname}:Combos";

            List<ComboSummary>? cachedCombos = await RedisService.GetAsync<List<ComboSummary>>(cacheKey);
            if (cachedCombos != null)
            {
                return cachedCombos;
            }

            IEnumerable<MealCombo> combos = await Repo.GetAsync<MealCombo>(
                filter: c => c.IsActive,
                includeProperties: "ComboDetails.Dish"
            );

            List<ComboSummary> result = combos.Select(MapToComboSummary).ToList();

            await RedisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }

        public async Task<ComboSummary> GetComboById(Guid id)
        {
            MealCombo combo = await Repo.GetOneAsync<MealCombo>(
                filter: c => c.Id == id,
                includeProperties: "ComboDetails.Dish"
            );

            if (combo == null)
            {
                throw new AppException("Combo not found");
            }

            return MapToComboSummary(combo);
        }

        public async Task<Guid> UpsertCombo(ComboSummary comboSummary)
        {
            MealCombo combo;

            if (comboSummary.Id == Guid.Empty)
            {
                string comboCode = await GenerateNextComboCodeAsync();

                string comboImageUrl = string.Empty;
                if (comboSummary.File != null)
                {
                    using Stream stream = comboSummary.File.OpenReadStream();

                    CloudinaryUploadResult uploadResult = await cloudinaryService.UploadAsync(
                        fileStream: stream,
                        fileName: comboSummary.File.FileName,
                        folder: $"{CurrentTenant.Name.Replace(" ", string.Empty)}/combos",
                        publicId: comboCode,
                        overwrite: true
                    );
                    comboImageUrl = uploadResult.Url;
                }

                combo = new MealCombo
                {
                    Name = comboSummary.Name.Trim(),
                    Code = comboCode,
                    Description = comboSummary.Description.Trim(),
                    Price = comboSummary.Price,
                    IsActive = comboSummary.IsActive,
                    ImageUrl = comboImageUrl,
                    BaseCost = await CalculateComboBaseCostAsync(comboSummary),
                    ComboDetails = (comboSummary.Details ?? new List<ComboDetailItem>())
                        .Select(d => new ComboDetail
                        {
                            ComboId = Guid.Empty, 
                            DishId = d.DishId,
                            Quantity = d.Quantity > 0 ? d.Quantity : 1
                        })
                        .ToList()
                };

                await Repo.CreateAsync(combo);
            }
            else
            {
                combo = await Repo.GetOneAsync<MealCombo>(
                    filter: x => x.Id == comboSummary.Id,
                    includeProperties: "ComboDetails"
                );

                if (combo == null)
                {
                    throw new AppException("Combo not found");
                }

                string comboImageUrl = combo.ImageUrl ?? string.Empty;

                if (comboSummary.File != null)
                {
                    string tenantFolder = CurrentTenant.Name.Replace(" ", string.Empty);
                    string cloudinaryPublicId = $"{tenantFolder}/combos/{combo.Code}";

                    await cloudinaryService.DeleteAsync(cloudinaryPublicId);

                    using Stream stream = comboSummary.File.OpenReadStream();

                    CloudinaryUploadResult uploadResult = await cloudinaryService.UploadAsync(
                        fileStream: stream,
                        fileName: comboSummary.File.FileName,
                        folder: $"{tenantFolder}/combos",
                        publicId: combo.Code,
                        overwrite: true
                    );

                    comboImageUrl = uploadResult.Url;
                }

                combo.Name = comboSummary.Name.Trim();
                combo.Description = comboSummary.Description.Trim();
                combo.Price = comboSummary.Price;
                combo.IsActive = comboSummary.IsActive;
                combo.ImageUrl = comboImageUrl;
                combo.BaseCost = await CalculateComboBaseCostAsync(comboSummary);

                if (combo.ComboDetails != null && combo.ComboDetails.Any())
                {
                    foreach (ComboDetail oldDetail in combo.ComboDetails.ToList())
                    {
                        Repo.Delete(oldDetail);
                    }

                    combo.ComboDetails.Clear();
                }

                List<ComboDetail> newComboDetails = (comboSummary.Details ?? new List<ComboDetailItem>())
                    .Select(d => new ComboDetail
                    {
                        ComboId = combo.Id,
                        DishId = d.DishId,
                        Quantity = d.Quantity > 0 ? d.Quantity : 1
                    })
                    .ToList();

                foreach (ComboDetail comboDetail in newComboDetails)
                {
                    combo.ComboDetails.Add(comboDetail);
                    await Repo.CreateAsync(comboDetail);
                }
            }

            try
            {
                await Repo.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new AppException("Combo was modified or deleted by another user. Please reload and try again.");
            }

            await RedisService.RemoveAsync($"{CurrentTenant.Hostname}:Combos");
            await RedisService.RemoveAsync($"{CurrentTenant.Hostname}:Menu");

            return combo.Id;
        }

        public async Task<bool> DeleteCombo(Guid id)
        {
            MealCombo combo = await Repo.GetOneAsync<MealCombo>(filter: c => c.Id == id);

            if (combo == null)
            {
                return false;
            }

            combo.IsActive = false;
            Repo.Update(combo);
            await Repo.SaveAsync();

            await RedisService.RemoveAsync($"{CurrentTenant.Hostname}:Combos");
            await RedisService.RemoveAsync($"{CurrentTenant.Hostname}:Menu");

            return true;
        }

        private async Task<string> GenerateNextComboCodeAsync()
        {
            int number = await Repo.GetCountAsync<MealCombo>() + 1;
            string code = $"CB{number:D2}";

            bool exists = await Repo.GetExistsAsync<MealCombo>(x => x.Code == code);
            while (exists)
            {
                number++;
                code = $"CB{number:D2}";
                exists = await Repo.GetExistsAsync<MealCombo>(x => x.Code == code);
            }

            return code;
        }

        private async Task<decimal> CalculateComboBaseCostAsync(ComboSummary comboSummary)
        {
            List<Guid> dishIds = (comboSummary.Details ?? new List<ComboDetailItem>())
                .Select(x => x.DishId)
                .Distinct()
                .ToList();

            if (!dishIds.Any())
            {
                return 0;
            }

            IEnumerable<Dish> dishes = await Repo.GetAsync<Dish>(filter: d => dishIds.Contains(d.Id));
            Dictionary<Guid, Dish> dishLookup = dishes.ToDictionary(x => x.Id, x => x);

            decimal baseCost = 0m;

            foreach (ComboDetailItem detail in comboSummary.Details)
            {
                if (!dishLookup.TryGetValue(detail.DishId, out Dish? dish))
                {
                    throw new AppException($"Dish '{detail.DishId}' not found");
                }

                int quantity = detail.Quantity > 0 ? detail.Quantity : 1;
                baseCost += dish.Price * quantity;
            }

            return baseCost;
        }

        private ComboSummary MapToComboSummary(MealCombo combo)
        {
            ComboSummary summary = mapper.Map<ComboSummary>(combo);

            summary.Details = combo.ComboDetails?
                .Select(d => new ComboDetailItem
                {
                    Id = d.Id,
                    DishId = d.DishId,
                    DishName = d.Dish?.Name,
                    DishPrice = d.Dish?.Price,
                    Quantity = d.Quantity
                })
                .ToList() ?? new List<ComboDetailItem>();

            return summary;
        }
    }
}



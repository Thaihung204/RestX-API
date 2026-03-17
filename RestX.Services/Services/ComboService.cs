using AutoMapper;
using CloudinaryDotNet.Actions;
using Microsoft.Data.SqlClient;
using RestX.BLL.DataTranferObjects.Combo;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using System.Data;
using System.Text;

namespace RestX.BLL.Services
{
    public class ComboService : BaseService, IComboService
    {
        private readonly IMapper mapper;
        private readonly ICloudinaryService cloudinaryService;

        public ComboService(
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.cloudinaryService = cloudinaryService;
            this.mapper = mapper;
        }

        public async Task<ComboSearchResult> GetAllCombos(ComboSearch model)
        {
            model ??= new ComboSearch();

            ComboSearchResult result = new ComboSearchResult();

            StringBuilder query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM dbo.MealCombos c
                WHERE 1 = 1
            ");

            List<SqlParameter> countParams = new List<SqlParameter>();
            List<SqlParameter> queryParams = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(model.SearchText))
            {
                query.Append(@"
                    AND (
                        c.Name LIKE '%' + @SearchText + '%'
                        OR c.Code LIKE '%' + @SearchText + '%'
                    )
                ");

                countParams.Add(new SqlParameter("SearchText", SqlDbType.NVarChar, 255) { Value = model.SearchText.Trim() });
                queryParams.Add(new SqlParameter("SearchText", SqlDbType.NVarChar, 255) { Value = model.SearchText.Trim() });
            }

            if (model.IsActive.HasValue)
            {
                query.Append(" AND c.IsActive = @IsActive ");
                countParams.Add(new SqlParameter("IsActive", SqlDbType.Bit) { Value = model.IsActive.Value });
                queryParams.Add(new SqlParameter("IsActive", SqlDbType.Bit) { Value = model.IsActive.Value });
            }

            if (model.PriceFrom.HasValue)
            {
                query.Append(" AND c.Price >= @PriceFrom ");
                countParams.Add(new SqlParameter("PriceFrom", SqlDbType.Decimal) { Value = model.PriceFrom.Value });
                queryParams.Add(new SqlParameter("PriceFrom", SqlDbType.Decimal) { Value = model.PriceFrom.Value });
            }

            if (model.PriceTo.HasValue)
            {
                query.Append(" AND c.Price <= @PriceTo ");
                countParams.Add(new SqlParameter("PriceTo", SqlDbType.Decimal) { Value = model.PriceTo.Value });
                queryParams.Add(new SqlParameter("PriceTo", SqlDbType.Decimal) { Value = model.PriceTo.Value });
            }

            string countQuery = query.ToString().Replace("#SELECT#", "COUNT(1)");
            int totalCount = await Repo.ExecuteSqlCommandAsync<int>(
                countQuery,
                countParams.Any() ? countParams.Cast<object>().ToArray() : null
            );

            result.TotalItems = totalCount;
            result.Page = model.Page;
            result.ItemsPerPage = model.ItemsPerPage;
            result.TotalPages = (int)Math.Ceiling((decimal)totalCount / model.ItemsPerPage);

            if (totalCount == 0)
            {
                result.Combos = new List<ComboSummary>();
                return result;
            }

            int skip = model.Page <= 1 ? 0 : (model.Page - 1) * model.ItemsPerPage;

            string selectItems = @"
                c.Id,
                c.Name,
                c.Code,
                c.Description,
                c.ImageUrl,
                c.BaseCost,
                c.Price,
                c.IsActive
            ";

            string mainQuery = query.ToString().Replace("#SELECT#", selectItems);

            mainQuery += model.SortBy switch
            {
                "name_asc" => " ORDER BY c.Name ASC",
                "name_desc" => " ORDER BY c.Name DESC",
                "price_asc" => " ORDER BY c.Price ASC",
                "price_desc" => " ORDER BY c.Price DESC",
                "created_asc" => " ORDER BY c.CreatedDate ASC",
                _ => " ORDER BY c.CreatedDate DESC"
            };

            mainQuery += $" OFFSET {skip} ROWS FETCH NEXT {model.ItemsPerPage} ROWS ONLY";

            List<ComboSummary> combos = await Repo.ExecuteSqlSelectAsync<ComboSummary>(
                mainQuery,
                queryParams.Any() ? queryParams.Cast<object>().ToArray() : null
            );

            List<Guid> comboIds = combos.Select(x => x.Id).ToList();

            IEnumerable<ComboDetail> details = await Repo.GetAsync<ComboDetail>(
                filter: x => comboIds.Contains(x.ComboId),
                includeProperties: "Dish"
            );

            Dictionary<Guid, List<ComboDetailItem>> detailLookup = details
                .GroupBy(x => x.ComboId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(d => new ComboDetailItem
                    {
                        Id = d.Id,
                        DishId = d.DishId,
                        DishName = d.Dish?.Name,
                        DishPrice = d.Dish?.Price,
                        Quantity = d.Quantity
                    }).ToList()
                );

            foreach (ComboSummary combo in combos)
            {
                combo.Details = detailLookup.TryGetValue(combo.Id, out List<ComboDetailItem>? comboDetails)
                    ? comboDetails
                    : new List<ComboDetailItem>();
            }

            result.Combos = combos;
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

            List<ComboSummary> result = combos.Select(MapToSummary).ToList();

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

            return MapToSummary(combo);
        }

        public async Task<Guid> UpsertCombo(ComboSummary comboSummary)
        {
            MealCombo combo;

            if (comboSummary.Id == Guid.Empty)
            {
                string comboCode = await GenerateNextComboCodeAsync();

                string comboImageUrl = String.Empty;
                if (comboSummary.File != null)
                {
                    using Stream stream = comboSummary.File.OpenReadStream();

                    var uploadResult = await cloudinaryService.UploadAsync(
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
                    BaseCost = await CalculateBaseCostAsync(comboSummary),
                    ComboDetails = (comboSummary.Details ?? new List<ComboDetailItem>())
                        .Select(d => new ComboDetail
                        {
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

                combo.ImageUrl = comboImageUrl;

                combo.Name = comboSummary.Name.Trim();
                combo.Description = comboSummary.Description.Trim();
                combo.Price = comboSummary.Price;
                combo.IsActive = comboSummary.IsActive;
                combo.ImageUrl = comboImageUrl;
                combo.BaseCost = await CalculateBaseCostAsync(comboSummary);

                if (combo.ComboDetails != null && combo.ComboDetails.Any())
                {
                    foreach (ComboDetail oldDetail in combo.ComboDetails.ToList())
                    {
                        Repo.Delete(oldDetail);
                    }
                }
                combo.ComboDetails = (comboSummary.Details ?? new List<ComboDetailItem>())
                    .Select(d => new ComboDetail
                    {
                        DishId = d.DishId,
                        Quantity = d.Quantity > 0 ? d.Quantity : 1
                    })
                    .ToList();
                Repo.Update(combo);
            }

            await Repo.SaveAsync();

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

        private async Task<decimal> CalculateBaseCostAsync(ComboSummary comboSummary)
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

        private ComboSummary MapToSummary(MealCombo combo)
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
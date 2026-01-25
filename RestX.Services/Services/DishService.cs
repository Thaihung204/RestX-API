using AutoMapper;
using Microsoft.Data.SqlClient;
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

        public DishService(IRepository repo, IRedisService redisService, IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
        }

        public async Task<DishSearchResult> GetAllDishes(DishSearch model)
        {
            var result = new DishSearchResult();

            var query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM dbo.Dishes d
                JOIN dbo.Categories c ON d.CategoryId = c.Id
                OUTER APPLY (
                    SELECT TOP (1) di.ImageUrl
                    FROM dbo.DishImages di
                    WHERE di.DishId = d.Id
                      AND di.IsActive = 1
                      AND di.ImageType = 0
                    ORDER BY di.DisplayOrder ASC, di.Id ASC
                ) mainImg
                WHERE 1 = 1
            ");

            var countParams = new List<SqlParameter>();
            var queryParams = new List<SqlParameter>();

            // Search: Name + Description
            if (!string.IsNullOrWhiteSpace(model.SearchText))
            {
                query.Append(@"
                    AND (
                        d.Name LIKE '%' + @SearchText + '%'
                        OR d.Description LIKE '%' + @SearchText + '%'
                    )
                ");

                countParams.Add(new SqlParameter("SearchText", SqlDbType.NVarChar, 2000)
                {
                    Value = model.SearchText
                });

                queryParams.Add(new SqlParameter("SearchText", SqlDbType.NVarChar, 2000)
                {
                    Value = model.SearchText
                });
            }

            // Category
            if (model.CategoryId.HasValue)
            {
                query.Append(" AND d.CategoryId = @CategoryId ");

                countParams.Add(new SqlParameter("CategoryId", SqlDbType.UniqueIdentifier)
                {
                    Value = model.CategoryId.Value
                });

                queryParams.Add(new SqlParameter("CategoryId", SqlDbType.UniqueIdentifier)
                {
                    Value = model.CategoryId.Value
                });
            }

            // Boolean filters
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

            // Price range
            if (model.PriceFrom.HasValue)
            {
                query.Append(" AND d.Price >= @PriceFrom ");

                countParams.Add(new SqlParameter("PriceFrom", SqlDbType.Decimal)
                {
                    Value = model.PriceFrom.Value
                });

                queryParams.Add(new SqlParameter("PriceFrom", SqlDbType.Decimal)
                {
                    Value = model.PriceFrom.Value
                });
            }

            if (model.PriceTo.HasValue)
            {
                query.Append(" AND d.Price <= @PriceTo ");

                countParams.Add(new SqlParameter("PriceTo", SqlDbType.Decimal)
                {
                    Value = model.PriceTo.Value
                });

                queryParams.Add(new SqlParameter("PriceTo", SqlDbType.Decimal)
                {
                    Value = model.PriceTo.Value
                });
            }

            // COUNT
            var countQuery = query.ToString()
                .Replace("#SELECT#", "COUNT(DISTINCT d.Id)");

            var totalCount = await Repo.ExecuteSqlCommandAsync<int>(
                countQuery,
                countParams.Any() ? countParams.Cast<object>().ToArray() : null
            );

            result.TotalCount = totalCount;
            result.Page = model.Page;
            result.ItemsPerPage = model.ItemsPerPage;
            result.TotalPages = (int)Math.Ceiling(
                (decimal)totalCount / model.ItemsPerPage
            );

            int skip = model.Page == 1 ? 0 : (model.Page - 1) * model.ItemsPerPage;

            // SELECT list
            var selectItems = @"
                DISTINCT
                d.Id,
                d.Name,
                c.Name AS CategoryName,
                d.Price,
                d.IsActive,
                d.CreatedDate,
                mainImg.ImageUrl AS MainImageUrl
            ";

            var mainQuery = countQuery.Replace(
                "COUNT(DISTINCT d.Id)",
                selectItems
            );

            // Sorting (whitelist)
            mainQuery += model.SortBy switch
            {
                "name_asc" => " ORDER BY d.Name ASC",
                "name_desc" => " ORDER BY d.Name DESC",
                "price_asc" => " ORDER BY d.Price ASC",
                "price_desc" => " ORDER BY d.Price DESC",
                "created_asc" => " ORDER BY d.CreatedDate ASC",
                _ => " ORDER BY d.CreatedDate DESC"
            };

            mainQuery += $" OFFSET {skip} ROWS FETCH NEXT {model.ItemsPerPage} ROWS ONLY";

            result.Dishes = await Repo.ExecuteSqlSelectAsync<DishItem>(
                mainQuery,
                queryParams.Any() ? queryParams.Cast<object>().ToArray() : null
            );

            return result;
        }

        public async Task<DishItem> GetDishById(Guid id)
        {
            var dish = (await Repo.GetAsync<Dish>(
                    filter: d => d.Id == id,
                    includeProperties: "Category,DishImages",
                    take: 1))
                .FirstOrDefault();

            var mainImageUrl = dish.DishImages?
                .Where(x => x.IsActive && x.ImageType == DishImageType.Main)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .Select(x => x.ImageUrl)
                .FirstOrDefault();

            return new DishItem
            {
                Id = dish.Id,
                Name = dish.Name,
                CategoryName = dish.Category?.Name ?? string.Empty,
                Price = dish.Price,
                IsActive = dish.IsActive,
                CreatedDate = dish.CreatedDate,
                Description = dish.Description,
                MainImageUrl = mainImageUrl
            };
        }

        public async Task<Dish> UpsertDish(Dish model)
        {
            if (model.Id != Guid.Empty)
            {
                var dish = await Repo.GetByIdAsync<Dish>(model.Id);
                if (dish == null)
                {
                    throw new InvalidOperationException("Dish not found");
                }

                dish.CategoryId = model.CategoryId;
                dish.Name = model.Name;
                dish.Description = model.Description;
                dish.Price = model.Price;
                dish.Unit = model.Unit;
                dish.Quantity = model.Quantity;
                dish.IsVegetarian = model.IsVegetarian;
                dish.IsSpicy = model.IsSpicy;
                dish.IsBestSeller = model.IsBestSeller;
                dish.IsActive = model.IsActive;
                dish.AutoDisableByStock = model.AutoDisableByStock;

                Repo.Update(dish);
                await Repo.SaveAsync();

                return dish;
            }

            await Repo.CreateAsync(model);
            return model;
        }

        public async Task DeleteDish(Guid id)
        {
            var dish = await GetDishById(id);
            if (dish != null)
            {
                Repo.Delete<Dish>(id);
                await Repo.SaveAsync();
            }
        }
    }
}
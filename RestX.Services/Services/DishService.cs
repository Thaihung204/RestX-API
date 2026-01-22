using Microsoft.Data.SqlClient;
using RestX.BLL.Interfaces;
using RestX.Models.Menu;
using System.Data;
using System.Text;

namespace RestX.BLL.Services
{
    public class DishService : BaseService, IDishService
    {
        private readonly IRepository _repo;

        public DishService(IRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<DishSearchResult> GetAllDishes(DishSearch model)
        {
            var result = new DishSearchResult();

            var query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM dbo.Dishes d
                JOIN dbo.Categories c ON d.CategoryId = c.Id
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

                var p = new SqlParameter("SearchText", SqlDbType.NVarChar, 2000)
                {
                    Value = model.SearchText
                };
                countParams.Add(p);
                queryParams.Add(p);
            }

            // Category
            if (model.CategoryId.HasValue)
            {
                query.Append(" AND d.CategoryId = @CategoryId ");
                var p = new SqlParameter("CategoryId", SqlDbType.UniqueIdentifier)
                {
                    Value = model.CategoryId.Value
                };
                countParams.Add(p);
                queryParams.Add(p);
            }

            // Boolean filters
            void AddBoolFilter(string column, string param, bool? value)
            {
                if (!value.HasValue) return;

                query.Append($" AND {column} = @{param} ");
                var p = new SqlParameter(param, SqlDbType.Bit) { Value = value.Value };
                countParams.Add(p);
                queryParams.Add(p);
            }

            AddBoolFilter("d.IsVegetarian", "IsVegetarian", model.IsVegetarian);
            AddBoolFilter("d.IsSpicy", "IsSpicy", model.IsSpicy);
            AddBoolFilter("d.IsBestSeller", "IsBestSeller", model.IsBestSeller);
            AddBoolFilter("d.IsActive", "IsActive", model.IsActive);

            // Price range
            if (model.PriceFrom.HasValue)
            {
                query.Append(" AND d.Price >= @PriceFrom ");
                var p = new SqlParameter("PriceFrom", SqlDbType.Decimal)
                {
                    Value = model.PriceFrom.Value
                };
                countParams.Add(p);
                queryParams.Add(p);
            }

            if (model.PriceTo.HasValue)
            {
                query.Append(" AND d.Price <= @PriceTo ");
                var p = new SqlParameter("PriceTo", SqlDbType.Decimal)
                {
                    Value = model.PriceTo.Value
                };
                countParams.Add(p);
                queryParams.Add(p);
            }

            // COUNT
            var countQuery = query.ToString()
                .Replace("#SELECT#", "COUNT(DISTINCT d.Id)");

            var totalCount = await Repo.ExecuteSqlCommandAsync<int>(
                countQuery,
                countParams.Any() ? countParams.ToArray() : null
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
                d.Name,
                c.Name AS CategoryName,
                d.Price,
                d.IsActive,
                d.CreatedDate
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
                queryParams.Any() ? queryParams.ToArray() : null
            );

            return result;
        }
        public async Task<DishSearchResult> GetDishes(DishSearch model)
        {
            var result = new DishSearchResult();

            var query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM dbo.Dishes d
                JOIN dbo.Categories c ON d.CategoryId = c.Id
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

                var p = new SqlParameter("SearchText", SqlDbType.NVarChar)
                {
                    Value = model.SearchText
                };
                countParams.Add(p);
                queryParams.Add(p);
            }

            // Category
            if (model.CategoryId.HasValue)
            {
                query.Append(" AND d.CategoryId = @CategoryId ");
                var p = new SqlParameter("CategoryId", SqlDbType.UniqueIdentifier)
                {
                    Value = model.CategoryId.Value
                };
                countParams.Add(p);
                queryParams.Add(p);
            }

            // Boolean filters
            void AddBoolFilter(string column, string param, bool? value)
            {
                if (!value.HasValue) return;

                query.Append($" AND {column} = @{param} ");
                var p = new SqlParameter(param, SqlDbType.Bit) { Value = value.Value };
                countParams.Add(p);
                queryParams.Add(p);
            }

            AddBoolFilter("d.IsVegetarian", "IsVegetarian", model.IsVegetarian);
            AddBoolFilter("d.IsSpicy", "IsSpicy", model.IsSpicy);
            AddBoolFilter("d.IsBestSeller", "IsBestSeller", model.IsBestSeller);
            AddBoolFilter("d.IsActive", "IsActive", model.IsActive);

            // Price range
            if (model.PriceFrom.HasValue)
            {
                query.Append(" AND d.Price >= @PriceFrom ");
                var p = new SqlParameter("PriceFrom", SqlDbType.Decimal)
                {
                    Value = model.PriceFrom.Value
                };
                countParams.Add(p);
                queryParams.Add(p);
            }

            if (model.PriceTo.HasValue)
            {
                query.Append(" AND d.Price <= @PriceTo ");
                var p = new SqlParameter("PriceTo", SqlDbType.Decimal)
                {
                    Value = model.PriceTo.Value
                };
                countParams.Add(p);
                queryParams.Add(p);
            }

            // COUNT
            var countQuery = query.ToString()
                .Replace("#SELECT#", "COUNT(DISTINCT d.Id)");

            var totalCount = await Repo.ExecuteSqlCommandAsync<int>(
                countQuery,
                countParams.Any() ? countParams.ToArray() : null
            );

            result.TotalCount = totalCount;
            result.Page = model.Page;
            result.ItemsPerPage = model.ItemsPerPage;
            result.TotalPages = (int)Math.Ceiling(
                (decimal)totalCount / model.ItemsPerPage
            );

            int skip = model.Page == 1
                ? 0
                : (model.Page - 1) * model.ItemsPerPage;

            // SELECT list
            var selectItems = @"
                DISTINCT
                d.Name,
                c.Name AS CategoryName,
                d.Price,
                d.IsActive,
                d.CreatedDate
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
                queryParams.Any() ? queryParams.ToArray() : null
            );

            return result;
        }

        public async Task<Dish?> GetDishById(Guid id)
        {
            return await _repo.GetByIdAsync<Dish>(id);
        }

        public async Task<Dish> UpsertDish(Dish model)
        {
            if (model.Id != Guid.Empty)
            {
                var dish = await _repo.GetByIdAsync<Dish>(model.Id);
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

                _repo.Update(dish);
                await _repo.SaveAsync();

                return dish;
            }

            await _repo.CreateAsync(model);
            await _repo.SaveAsync();
            return model;
        }

        public async Task DeleteDish(Guid id)
        {
            var dish = await GetDishById(id);
            if (dish != null)
            {
                _repo.Delete<Dish>(id);
                await _repo.SaveAsync();
            }
        }
    }
}
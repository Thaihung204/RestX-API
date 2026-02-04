using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace RestX.BLL.Helpers
{
    public class PaginationQueryBuilder
    {
        private readonly StringBuilder query;
        private readonly List<SqlParameter> countParams;
        private readonly List<SqlParameter> queryParams;
        public PaginationQueryBuilder(string baseQuery)
        {
            query = new StringBuilder(baseQuery);
            countParams = new List<SqlParameter>();
            queryParams = new List<SqlParameter>();
        }
        public PaginationQueryBuilder AddCondition(
            string condition,
            string paramName,
            object? value,
            SqlDbType dbType,
            bool addIfNotEmpty = true)
        {
            if (value == null) return this;
            if (addIfNotEmpty && value is string str && string.IsNullOrEmpty(str)) return this;

            query.Append($" AND {condition} ");
            countParams.Add(new SqlParameter(paramName, dbType) { Value = value });
            queryParams.Add(new SqlParameter(paramName, dbType) { Value = value });

            return this;
        }
        public PaginationQueryBuilder AddBoolCondition(string condition, string paramName, bool? value)
        {
            if (!value.HasValue) return this;
            return AddCondition(condition, paramName, value.Value, SqlDbType.Bit);
        }
        public PaginationQueryBuilder AddIntCondition(string condition, string paramName, int? value)
        {
            if (!value.HasValue) return this;
            return AddCondition(condition, paramName, value.Value, SqlDbType.Int);
        }
        public PaginationQueryBuilder AddDateCondition(string condition, string paramName, DateTime? value)
        {
            if (!value.HasValue) return this;
            return AddCondition(condition, paramName, value.Value, SqlDbType.DateTime);
        }
        public PaginationQueryBuilder AddLikeCondition(string condition, string paramName, string? value)
        {
            if (string.IsNullOrEmpty(value)) return this;
            return AddCondition(condition, paramName, value, SqlDbType.NVarChar);
        }
        public PaginationQueryBuilder AddSearchCondition(string[] columns, string paramName, string? value)
        {
            if (string.IsNullOrEmpty(value)) return this;

            var conditions = columns.Select(col => $"{col} LIKE '%' + @{paramName} + '%'");
            var searchClause = $" AND ({string.Join(" OR ", conditions)}) ";

            query.Append(searchClause);
            countParams.Add(new SqlParameter(paramName, SqlDbType.NVarChar) { Value = value });
            queryParams.Add(new SqlParameter(paramName, SqlDbType.NVarChar) { Value = value });

            return this;
        }
        public (string Query, SqlParameter[] Parameters) BuildCountQuery(string countSelect)
        {
            var countQuery = query.ToString().Replace("#SELECT#", countSelect);
            return (countQuery, countParams.ToArray());
        }
        public (string Query, SqlParameter[] Parameters) BuildDataQuery(
            string selectColumns,
            string orderByClause,
            int pageNumber,
            int pageSize)
        {
            int skip = pageNumber == 1 ? 0 : (pageNumber - 1) * pageSize;

            var dataQuery = query.ToString().Replace("#SELECT#", selectColumns);
            dataQuery += orderByClause;
            dataQuery += $" OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            return (dataQuery, queryParams.ToArray());
        }
    }
}

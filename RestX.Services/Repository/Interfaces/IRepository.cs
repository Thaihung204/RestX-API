using RestX.Models.Interfaces;

namespace RestX.BLL.Repository.Interfaces
{
    public interface IRepository : IReadOnlyRepository
    {
        object Create<TEntity>(TEntity entity, string createdBy = null)
            where TEntity : class, IEntity;

        Task<object> CreateAsync<TEntity>(TEntity entity, string createdBy = null)
            where TEntity : class, IEntity;

        void Update<TEntity>(TEntity entity, string modifiedBy = null)
            where TEntity : class, IEntity;

        void Delete<TEntity>(object id)
            where TEntity : class, IEntity;

        void Delete<TEntity>(TEntity entity)
            where TEntity : class, IEntity;

        void Save();
        Task SaveAsync();
        Task<T> ExecuteSqlCommandAsync<T>(string query, params object[] parameters);
        Task<T> ExecuteStoredProcedureAsync<T>(string storedProcedure, params object[] parameters);
        Task<List<T>> ExecuteSqlSelectAsync<T>(string query, object[] parameters = null, int commandTimeout = 600) where T : new();
        Task<int> ExecuteNonQueryAsync(string query, object[] parameters = null, int commandTimeout = 600);
    }
}

using RestX.AdminDAL.Context;
using RestX.BLL.Interfaces;
using RestX.DAL.Context;
using RestX.Models.Tenants;


namespace RestX.BLL.Services
{
    public class BaseService
    {
        public readonly IRepository Repo;
        public IRedisService RedisService;
        public readonly ActiveTenant CurrentTenant;

        public BaseService(IRepository repo, IRedisService redisService, IEnumerable<ActiveTenant> tenant = null)
        {
            this.Repo = repo;
            this.RedisService = redisService;
            this.CurrentTenant = tenant?.FirstOrDefault();
        }

        public BaseService()
        {
        }
    }
}
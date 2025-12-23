using RestX.BLL.Services.Interfaces;
using RestX.DAL.Context;
using RestX.Models.Admin;


namespace RestX.BLL.Services.Services
{
    public class BaseService
    {
        protected readonly IRepository Repo;
        private RestxAdminContext? Restxadmincontext;

        //protected RestxAdminContext Restxadmincontext
        //{
        //    get
        //    {
        //        if (Restxadmincontext == null)
        //        {
        //            Restxadmincontext = _httpContextAccessor.HttpContext?.Items["RestaurantContext"] as RestaurantContext ?? new RestaurantContext();
        //        }
        //        return restaurantContext;
        //    }
        //}

        //protected Guid OwnerId => RestaurantContext.OwnerId;
        //protected Guid? StaffId => RestaurantContext.StaffId;
        //protected int TableId => RestaurantContext.TableId;

        public BaseService(IRepository repo)
        {
            this.Repo = repo;
        }

        //public BaseService(IRepository repo, IHttpContextAccessor httpContextAccessor)
        //{
        //    this.Repo = repo;
        //    _httpContextAccessor = httpContextAccessor;
        //}
    }
}
using RestX.BLL.Services.Interfaces;


namespace RestX.BLL.Services.Services
{
    public class BaseService
    {
        //protected readonly IRepository Repo;
        //private readonly IHttpContextAccessor _httpContextAccessor;
        //private RestaurantContext? restaurantContext;

        //protected RestaurantContext RestaurantContext
        //{
        //    get
        //    {
        //        if (restaurantContext == null)
        //        {
        //            restaurantContext = _httpContextAccessor.HttpContext?.Items["RestaurantContext"] as RestaurantContext ?? new RestaurantContext();
        //        }
        //        return restaurantContext;
        //    }
        //}

        //protected Guid OwnerId => RestaurantContext.OwnerId;
        //protected Guid? StaffId => RestaurantContext.StaffId;
        //protected int TableId => RestaurantContext.TableId;

        //public BaseService(IRepository repo)
        //{
        //    this.Repo = repo;
        //}

        //public BaseService(IRepository repo, IHttpContextAccessor httpContextAccessor)
        //{
        //    this.Repo = repo;
        //    _httpContextAccessor = httpContextAccessor;
        //}
    }
}
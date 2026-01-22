
namespace RestX.BLL.Helpers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using RestX.BLL.Interfaces;
    using RestX.BLL.Services;
    using RestX.DAL.Context;

    public static class DIHelper
    {
        public static void Setup(IServiceCollection services, bool isDevelopment = false)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IRepository, EntityFrameworkRepository<TenantDbContext>>();
            services.AddScoped<IExceptionHandler, ExceptionHandler>();
            services.AddScoped<IDishService, DishService>();            
        }
    }
}

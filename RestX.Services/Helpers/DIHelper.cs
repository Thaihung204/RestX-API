
namespace RestX.BLL.Helpers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    public static class DIHelper
    {
        public static void Setup(IServiceCollection services, bool isDevelopment = false)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            
        }
    }
}

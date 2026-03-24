using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace RestX.BLL.Authentication
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HangfireAuthorizationFilter(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool Authorize(DashboardContext context)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        //public bool Authorize(DashboardContext context)
        //{
        //    var httpContext = context.GetHttpContext();
        //    return httpContext?.User?.Identity?.IsAuthenticated ?? false;
        //}
    }
}

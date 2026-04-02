using Hangfire.Dashboard;

namespace RestX.BLL.Authentication
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public HangfireAuthorizationFilter()
        {
        }

        public bool Authorize(DashboardContext context)
        {
            return true;
            //var httpContext = context.GetHttpContext();
            //return httpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }
}

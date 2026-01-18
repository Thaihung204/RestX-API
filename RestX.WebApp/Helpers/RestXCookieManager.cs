using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace RestX.App.Helpers
{
    public class RestXCookieManager : ICookieManager
    {
        private readonly ICookieManager ConcreteManager;
        public RestXCookieManager()
        {
            ConcreteManager = new ChunkingCookieManager();
        }
        public void AppendResponseCookie(HttpContext context, string key, string value, CookieOptions options)
        {
            options.Domain = context.Request.Host.Host;
            options.SameSite = SameSiteMode.None;
            ConcreteManager.AppendResponseCookie(context, key, value, options);
        }

        public void DeleteCookie(HttpContext context, string key, CookieOptions options)
        {
            options.Domain = context.Request.Host.Host;
            ConcreteManager.DeleteCookie(context, key, options);
        }

        public string GetRequestCookie(HttpContext context, string key)
        {
            return ConcreteManager.GetRequestCookie(context, key);
        }
    }
}

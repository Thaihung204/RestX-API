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

        public void SetTokenCookies(HttpContext context, string accessToken, DateTime accessTokenExpiry, string refreshToken)
        {
            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = accessTokenExpiry
            };
            AppendResponseCookie(context, "access_token", accessToken, accessTokenOptions);

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };
            AppendResponseCookie(context, "refresh_token", refreshToken, refreshTokenOptions);
        }

        public void ClearTokenCookies(HttpContext context)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true
            };
            DeleteCookie(context, "access_token", options);
            DeleteCookie(context, "refresh_token", options);
        }
    }
}

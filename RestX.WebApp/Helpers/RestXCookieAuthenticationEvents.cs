using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;
using RestX.Models.Tenants;

namespace RestX.App.Helpers
{
    public class RestXCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        public readonly IEnumerable<ActiveTenant> tenants;

        public RestXCookieAuthenticationEvents(IEnumerable<ActiveTenant> tenants)
        {
            this.tenants = tenants;
        }
        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> redirectContext)
        {
            if (redirectContext.Request.Path.StartsWithSegments("/api"))
            {
                redirectContext.Response.Clear();
                redirectContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            var uri = new Uri(redirectContext.RedirectUri);
            redirectContext.RedirectUri = string.Concat(tenants.FirstOrDefault()?.Hostname, "/login", uri.Query);
            return base.RedirectToLogin(redirectContext);
        }
    }
}

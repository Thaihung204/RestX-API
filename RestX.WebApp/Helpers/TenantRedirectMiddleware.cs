using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Internal;
using RestX.Models.Tenants;
using SaasKit.Multitenancy;

namespace RestX.BLL.MultiTenancy
{
    public class TenantRedirectMiddleware<TTenant>
    {
        private readonly RequestDelegate next;

        public TenantRedirectMiddleware(
            RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Ensure.Argument.NotNull(context, nameof(context));

            var tenantContext = context.GetTenant<ActiveTenant>();

            // Global redirect
            if (tenantContext != null && !string.IsNullOrEmpty(tenantContext.Hostname))
            {
                Redirect(context, tenantContext.Hostname, 1);
                return;
            }

            // Specific redirect
            if (context.Request.Path.HasValue && tenantContext != null)
            {
                var redirect =
                    tenantContext.Hostname.Where(r => r.ToString() == context.Request.Path.Value);
                if (redirect != null)
                {
                    this.Redirect(context, redirect.ToString(), 1);
                    return;
                }
            }
            
            // Otherwise continue processing
            await next(context);
        }
        private void Redirect(HttpContext context, string redirectLocation, int? type)
        {
            context.Response.Redirect(redirectLocation);
            context.Response.StatusCode = type ?? StatusCodes.Status301MovedPermanently;
        }
    }
}

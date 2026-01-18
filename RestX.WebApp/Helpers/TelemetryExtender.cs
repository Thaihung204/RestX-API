using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using RestX.Models.Tenants;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RestX.App.Helpers
{
    public class TelemetryExtender : IMiddleware
    {
        private readonly ActiveTenant currentTenant = null;
        public TelemetryExtender(IEnumerable<ActiveTenant> tenant = null)
        {
            this.currentTenant = tenant?.FirstOrDefault();
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var requestTelemetry = context.Features.Get<RequestTelemetry>();

            if (currentTenant != null)
            {
                if (!requestTelemetry.Properties.ContainsKey("TenantId"))
                {
                    requestTelemetry?.Properties.Add("TenantId", currentTenant?.Id.ToString());
                }
                if (!requestTelemetry.Properties.ContainsKey("TenantName"))
                {
                    requestTelemetry?.Properties.Add("TenantName", currentTenant?.Name);
                }
            }

            // Add the current user name and iod if visible
            if (context.User.Identity.IsAuthenticated)
            {
                if (!requestTelemetry.Properties.ContainsKey("UserName"))
                {
                    requestTelemetry?.Properties.Add("UserName", context.User.Identity.Name);
                }
                if (!requestTelemetry.Properties.ContainsKey("UserId"))
                {
                    requestTelemetry?.Properties.Add("UserId", context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value);
                }
            }

            // Check whether the url contains /api/ or not
            if (!requestTelemetry.Properties.ContainsKey("ApiRequest"))
            {
                requestTelemetry?.Properties.Add("ApiRequest", context.Request.Path.Value.Contains("/api/") ? "true" : "false");
            }
            if (!requestTelemetry.Properties.ContainsKey("ApiRequestFromApp"))
            {
                requestTelemetry?.Properties.Add("ApiRequestFromApp", context.Request.Headers.ContainsKey("tp-id") ? "true" : "false");
            }

            // Call next middleware in the pipeline
            await next(context);
        }
    }
}

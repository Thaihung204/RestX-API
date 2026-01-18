
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace RestX.BLL.MultiTenancy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using RestX.AdminDAL.Context;
    using RestX.BLL.Interfaces;
    using RestX.DAL.Context;
    using RestX.Models.Tenants;
    using SaasKit.Multitenancy;

    public class TenantResolver : DistributedCacheTenantResolver<ActiveTenant>
    {
        protected readonly ILogger log;
        private readonly RestxAdminContext _dbContext;

        public TenantResolver(IRedisService redisService, ILoggerFactory loggerFactory, IOptions<MultiTenancyOptions> options, RestxAdminContext dbContext)
            : base(redisService, loggerFactory)
        {
            this._dbContext = dbContext;
            this.log = loggerFactory.CreateLogger<TenantResolver>();
        }

        protected override DistributedCacheEntryOptions CreateCacheEntryOptions()
        {
            return new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(new TimeSpan(0, 100, 0));
        }

        public override List<string> GetContextIdentifier(HttpContext context)
        {
            var identifiers = new List<string>();
            identifiers.Add(context.Request.Host.Value.ToLower());
            if (context.Request.Path.HasValue)
            {
                var pathParams = context.Request.Path.Value.Split('/');
                if (pathParams.Length > 1 && !string.IsNullOrEmpty(pathParams[1]))
                {
                    identifiers.Add(context.Request.Host.Value.ToLower() + "/" + pathParams[1].ToLower());
                }
            }
            return identifiers;
        }

        public override IEnumerable<string> GetTenantIdentifiers(TenantContext<ActiveTenant> context)
        {
            return new[] { context.Tenant.Hostname };
        }


        public override Task<TenantContext<ActiveTenant>> ResolveAsync(HttpContext context)
        {
            TenantContext<ActiveTenant> tenantContext = null;
            var hostname = context.Request.Host.Value.ToLower();
            var hostnameWithPath = "";
            try
            {
                hostnameWithPath = context.Request.Path.HasValue && context.Request.Path.Value.Length > 1
                    ? string.Concat(context.Request.Host.Value.ToLower(), "/",
                        (context.Request.Path.Value.Substring(1).Contains("/")
                            ? context.Request.Path.Value.Substring(1,
                                context.Request.Path.Value.Substring(1).IndexOf("/", StringComparison.Ordinal))
                            : context.Request.Path.Value.Substring(1)))
                    : string.Empty;
            }
            catch (Exception ex)
            {
                // Catch here to ensure that the application does not crash if the path is not valid
                log.LogError(ex, $"Error getting 'hostnameWithPath', full URL: {context.Request.GetDisplayUrl()}");
            }

            var activeTenant = _dbContext.Tenants
                .FirstOrDefault(b =>
                    b.Hostname == hostname ||
                    (!string.IsNullOrEmpty(hostnameWithPath) && b.Hostname == hostnameWithPath)
                );

            ActiveTenant tenant = null;
            if (activeTenant != null)
            {
                //var activeTenant = this._dbContext.Tenants.FirstOrDefault(t => t.Id == activeBrand.AppTenantId);
                //var otherBrands = this._dbContext.TenantBrands.Where(b => b.AppTenantId == activeTenant.Id);

                // Map the active brand to the tenant
                tenant = new ActiveTenant()
                {
                    Id = activeTenant.Id,
                    Name = activeTenant.Name,
                    ConnectionString = activeTenant.ConnectionString,
                    ModifiedDate = activeTenant.ModifiedDate,
                    BaseColor = activeTenant.BaseColor,
                    PrimaryColor = activeTenant.PrimaryColor,
                    SecondaryColor = activeTenant.SecondaryColor,
                    HeaderColor = activeTenant.HeaderColor,
                    FooterColor = activeTenant.FooterColor,
                    FaviconUrl = activeTenant.FaviconUrl,
                    LogoUrl = activeTenant.LogoUrl,
                    Hostname = activeTenant.Hostname,
                    Prefix = activeTenant.Prefix,
                    BusinessName = activeTenant.BusinessName,
                    BusinessPrimaryPhone = activeTenant.BusinessPrimaryPhone,
                    BusinessSecondaryPhone = activeTenant.BusinessSecondaryPhone,
                    BusinessEmailAddress = activeTenant.BusinessEmailAddress,
                    BusinessCompanyNumber = activeTenant.BusinessCompanyNumber,
                    BusinessOpeningHours = activeTenant.BusinessOpeningHours,
                    BusinessAddressLine1 = activeTenant.BusinessAddressLine1,
                    BusinessAddressLine2 = activeTenant.BusinessAddressLine2,
                    BusinessAddressLine3 = activeTenant.BusinessAddressLine3,
                    BusinessAddressLine4 = activeTenant.BusinessAddressLine4,
                    BusinessCounty = activeTenant.BusinessCounty,
                    BusinessCountry = activeTenant.BusinessCountry,
                    BusinessPostCode = activeTenant.BusinessPostCode,
                };
            }

            if (tenant != null)
            {
                tenantContext = new TenantContext<ActiveTenant>(tenant);
            }

            return Task.FromResult(tenantContext);
        }
    }
}
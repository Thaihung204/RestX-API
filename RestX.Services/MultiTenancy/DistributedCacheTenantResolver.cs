using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using SaasKit.Multitenancy;
using RestX.BLL.Interfaces;

namespace RestX.BLL.MultiTenancy
{
    public abstract class DistributedCacheTenantResolver<TTenant> : ITenantResolver<TTenant>
    {
        protected readonly IRedisService redisService;
        protected readonly ILogger log;
        protected readonly MemoryCacheTenantResolverOptions options;

        public DistributedCacheTenantResolver(IRedisService redisService, ILoggerFactory loggerFactory)
            : this(redisService, loggerFactory, new MemoryCacheTenantResolverOptions())
        {
        }

        public DistributedCacheTenantResolver(IRedisService redisService, ILoggerFactory loggerFactory, MemoryCacheTenantResolverOptions options)
        {
            Ensure.Argument.NotNull(redisService, nameof(redisService));
            Ensure.Argument.NotNull(loggerFactory, nameof(loggerFactory));
            Ensure.Argument.NotNull(options, nameof(options));

            this.redisService = redisService;
            this.log = loggerFactory.CreateLogger<MemoryCacheTenantResolver<TTenant>>();
            this.options = options;
        }

        protected virtual DistributedCacheEntryOptions CreateCacheEntryOptions()
        {
            return new DistributedCacheEntryOptions()
                .SetSlidingExpiration(new TimeSpan(1, 0, 0));
        }

        protected virtual void DisposeTenantContext(object cacheKey, TenantContext<TTenant> tenantContext)
        {
            if (tenantContext != null)
            {
                log.LogDebug("Disposing TenantContext:{id} instance with key \"{cacheKey}\".", tenantContext.Id, cacheKey);
                tenantContext.Dispose();
            }
        }

        public abstract List<string> GetContextIdentifier(HttpContext context);
        public abstract IEnumerable<string> GetTenantIdentifiers(TenantContext<TTenant> context);
        public abstract Task<TenantContext<TTenant>> ResolveAsync(HttpContext context);

        async Task<TenantContext<TTenant>> ITenantResolver<TTenant>.ResolveAsync(HttpContext context)
        {
            Ensure.Argument.NotNull(context, nameof(context));

            // Obtain the key used to identify cached tenants from the current request
            var cacheKeys = GetContextIdentifier(context);

            if (cacheKeys.Count == 0)
            {
                return null;
            }

            TenantContext<TTenant> tenantContext;
            var tenantRawData = "";
            foreach(var cacheKey in cacheKeys)
            {
                tenantRawData = await redisService.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(tenantRawData))
                {
                    break;
                }
            }
                  
            if (string.IsNullOrEmpty(tenantRawData))
            {
                log.LogDebug("TenantContext not present in cache with keys \"{cacheKey}\". Attempting to resolve.", string.Join(", ", cacheKeys));
                tenantContext = await ResolveAsync(context);

                if (tenantContext != null)
                {
                    var tenantIdentifiers = GetTenantIdentifiers(tenantContext);

                    if (tenantIdentifiers != null)
                    {
                        var cacheEntryOptions = GetCacheEntryOptions();

                        log.LogDebug("TenantContext:{id} resolved. Caching with keys \"{tenantIdentifiers}\".", tenantContext.Id, tenantIdentifiers);

                        foreach (var identifier in tenantIdentifiers)
                        {
                            await redisService.SetStringAsync(identifier, JsonConvert.SerializeObject(tenantContext, new JsonSerializerSettings{ReferenceLoopHandling = ReferenceLoopHandling.Ignore}), TimeSpan.FromHours(2));
                        }
                    }
                }
            }
            else
            {
                tenantContext = JsonConvert.DeserializeObject<TenantContext<TTenant>>(tenantRawData);
                log.LogDebug("TenantContext:{id} retrieved from cache with keys \"{cacheKey}\".", tenantContext.Id, string.Join(", ", cacheKeys));
            }

            return tenantContext;
        }

        private DistributedCacheEntryOptions GetCacheEntryOptions()
        {
            var cacheEntryOptions = CreateCacheEntryOptions();

            if (options.EvictAllEntriesOnExpiry)
            {
                
            }

            if (options.DisposeOnEviction)
            {
                
            }

            return cacheEntryOptions;
        }
    }
}
